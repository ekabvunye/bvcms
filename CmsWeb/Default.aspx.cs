﻿using System;
using System.Linq;
using System.Xml.Linq;
using System.Data.Linq.SqlClient;
using CmsData;
using UtilityExtensions;
using System.Net;
using System.IO;

namespace CMSWeb
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            BindBirthdays();
            BindMyInvolvements();
            BindNews();
        }
        private void BindBirthdays()
        {
            var user = DbUtil.Db.CurrentUser;
            if (user == null || user.Person == null)
                return;
            var n = UtilityExtensions.Util.Now;
            var tag = DbUtil.Db.FetchOrCreateTag("TrackBirthdays", user.PeopleId, DbUtil.TagTypeId_Personal);
            var q = tag.People();
            if (q.Count() == 0)
                q = from p in DbUtil.Db.People
                    where p.OrganizationMembers.Any(om => om.OrganizationId == user.Person.BibleFellowshipClassId)
                    select p;
            var org = DbUtil.Db.Organizations.SingleOrDefault(o => o.OrganizationId == user.Person.BibleFellowshipClassId);
            BFClass.Visible = org != null;
            if (BFClass.Visible)
            {
                BFClass.Text = org.FullName;
                BFClass.NavigateUrl = "~/Organization.aspx?id=" + org.OrganizationId;
            }

            var q2 = from p in q
                     let nextbd = DbUtil.Db.NextBirthday(p.PeopleId)
                     where SqlMethods.DateDiffDay(UtilityExtensions.Util.Now, nextbd) <= 15
                     orderby nextbd
                     select new { Birthday = nextbd, Name = p.Name, Id = p.PeopleId };
            Birthdays.DataSource = q2;
            Birthdays.DataBind();
        }
        private void BindMyInvolvements()
        {
            var u = DbUtil.Db.Users.SingleOrDefault(us => us.UserId == Util.UserId);
            if (u == null)
                return;

            if (u.PeopleId.HasValue)
            {
                var q = from om in DbUtil.Db.OrganizationMembers
                        where om.PeopleId == u.PeopleId
                        where (om.Pending ?? false) == false
                        where !(om.Organization.SecurityTypeId == 3 && Util.OrgMembersOnly)
                        select new 
                        { 
                            Name = om.Organization.OrganizationName, 
                            MemberType = om.MemberType.Description,
                            Id = om.OrganizationId
                        };
                grdMyInvolvement.DataSource = q;
                grdMyInvolvement.DataBind();
                grdMyInvolvement.Visible = true;
            }
            else
                grdMyInvolvement.Visible = false;
        }
        private void BindNews()
        {
            var feedurl = DbUtil.Settings("BlogFeedUrl", "http://feeds2.feedburner.com/Bvcms-Blog");

            BlogLink.NavigateUrl = DbUtil.Settings("BlogAppUrl", "http://www.bvcms.com/blog/");

            var wr = new WebClient();
            var feed = DbUtil.Db.RssFeeds.FirstOrDefault(r => r.Url == feedurl);

            HttpWebRequest req = null;

            try
            {
                req = WebRequest.Create(feedurl) as HttpWebRequest;
            }
            catch
            {
            }

            if (feed != null)
            {
                req.IfModifiedSince = feed.LastModified.Value;
                req.Headers.Add("If-None-Match", feed.ETag);
            }
            else
            {
                feed = new RssFeed();
                DbUtil.Db.RssFeeds.InsertOnSubmit(feed);
                feed.Url = feedurl;
            }

            if (req != null)
            {
                try
                {
                    var resp = req.GetResponse() as HttpWebResponse;
                    feed.LastModified = resp.LastModified;
                    feed.ETag = resp.Headers["ETag"];
                    var sr = new StreamReader(resp.GetResponseStream());
                    feed.Data = sr.ReadToEnd();
                    sr.Close();
                    DbUtil.Db.SubmitChanges();
                }
                catch (WebException)
                {
                }
                XDocument rssFeed = XDocument.Parse(feed.Data);

                var posts = from item in rssFeed.Descendants("item")
                            let au = item.Element("author")
                            select new
                            {
                                Title = item.Element("title").Value,
                                Published = DateTime.Parse(item.Element("pubDate").Value),
                                Url = item.Element("link").Value,
                                Author = au != null ? au.Value : "David Carroll",
                            };

                NewsGrid.DataSource = posts;
                NewsGrid.DataBind();
           }
        }
    }
}
