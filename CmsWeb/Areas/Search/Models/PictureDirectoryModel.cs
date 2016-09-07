/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CmsData;
using CmsWeb.Areas.Search.Controllers;
using CmsWeb.Models;
using HandlebarsDotNet;
using UtilityExtensions;

namespace CmsWeb.Areas.Search.Models
{
    public class PictureDirectoryModel : PagedTableModel<Person, dynamic>
    {
        public string StatusFlag;
        public int? OrgId;
        public bool CanView;
        private string OrderBy { get; set; }
        public bool HasAccess { get; set; }
        private static string Setting => DbUtil.Db.Setting("PictureDirectorySelector", "");
        private string Sql { get; set; }
        private string Template { get; set; }
        private string TemplateName { get; set; }

        public PictureDirectoryModel()
        {
            Initialize();
        }

        public PictureDirectoryModel(int? orgId)
            : base("Name", "asc")
        {
            OrgId = orgId;
            Initialize();
        }

        private void Initialize()
        {
            AjaxPager = true;
            HasAccess = HttpContext.Current.User.IsInRole("Access");
            GetCount = CountEntries;
            if (OrgId == null)
                if (Regex.IsMatch(Setting, @"\AF\d\d\z"))
                    StatusFlag = Setting;
                else if (Regex.IsMatch(Setting, @"\A\d+\z"))
                    OrgId = Setting.ToInt();
            TemplateName = "PictureDirectoryTemplate";
            if (OrgId.HasValue)
            {
                var inorg = (from om in DbUtil.Db.OrganizationMembers
                             where om.PeopleId == Util.UserPeopleId
                             where om.OrganizationId == OrgId
                             where om.Organization.PublishDirectory > 0
                             where !om.OrgMemberExtras.Any(vv => vv.BitValue == true && vv.Field == "DoNotPublish")
                             where om.OrgMemMemTags.All(vv => vv.MemberTag.Name != "DoNotPublish")
                             select om).Any();
                CanView = inorg || HttpContext.Current.User.IsInRole("Admin");
                Template = DbUtil.Db.OrganizationExtras.SingleOrDefault(vv => vv.OrganizationId == OrgId && vv.Field == TemplateName)?.Data
                    ?? DbUtil.Db.ContentText(TemplateName, Resource1.PictureDirectoryTemplate);
                var sqlname = DbUtil.Db.OrganizationExtras.SingleOrDefault(vv => vv.OrganizationId == OrgId && vv.Field == "PictureDirectorySql")?.Data;
                Sql = DbUtil.Db.ContentSql(sqlname ?? "PictureDirectory", Resource1.PictureDirectorySql);
            }
            else if (StatusFlag.HasValue())
            {
                var hasstatus = (from v in DbUtil.Db.StatusFlags(StatusFlag)
                                 where v.PeopleId == Util.UserPeopleId
                                 select v).Any();
                CanView = hasstatus || HttpContext.Current.User.IsInRole("Admin");
                Template = DbUtil.Db.ContentText(TemplateName, Resource1.PictureDirectoryTemplate);
                Sql = DbUtil.Db.ContentSql("PictureDirectory", Resource1.PictureDirectorySql);
            }
        }

        public string Name { get; set; }
        public bool FamilyOption { get; set; }

        private List<dynamic> entries;
        public IEnumerable<dynamic> Entry => entries ?? (entries = ViewList().ToList());

        public int CountEntries()
        {
            if (!count.HasValue)
                count = DefineModelList().Count();
            return count.Value;
        }

        public override IQueryable<Person> DefineModelList()
        {
            IQueryable<Person> qmembers;
            if (!CanView)
                qmembers = DbUtil.Db.PeopleQuery2("PeopleId = 0");
            else if (StatusFlag.HasValue())
                qmembers = DbUtil.Db.PeopleQuery2($"StatusFlag = '{StatusFlag}'");
            else if (OrgId.HasValue)
                qmembers = DbUtil.Db.PeopleQuery2($"IsMemberOf( Org={OrgId} ) = 1");
            else
                qmembers = DbUtil.Db.PeopleQuery2("PeopleId = 0");

            if (Name.HasValue())
                qmembers = from p in qmembers
                           where p.Name.Contains(Name)
                           select p;
            return qmembers;
        }

        public override IQueryable<Person> DefineModelSort(IQueryable<Person> q)
        {
            if (Direction == "asc")
                switch (Sort)
                {
                    case "Name":
                        q = from p in q
                            orderby p.Name2
                            select p;
                        OrderBy = "ORDER BY p.Name2";
                        break;
                    case "Birthday":
                        q = from p in q
                            orderby DbUtil.Db.NextBirthday(p.PeopleId)
                            select p;
                        OrderBy = "ORDER BY dbo.NextBirthday(p.PeopleId)";
                        break;
                }
            else
            {
                switch (Sort)
                {
                    case "Name":
                        q = from p in q
                            orderby p.Name2 descending
                            select p;
                        OrderBy = "ORDER BY p.Name2 DESC";
                        break;
                    case "Birthday":
                        q = from p in q
                            orderby DbUtil.Db.NextBirthday(p.PeopleId) descending
                            select p;
                        OrderBy = "ORDER BY dbo.NextBirthday(p.PeopleId) DESC";
                        break;
                }
            }
            return q;
        }

        public override IEnumerable<dynamic> DefineViewList(IQueryable<Person> q)
        {
            var tagid = DbUtil.Db.PopulateTemporaryTag(q.Select(vv => vv.PeopleId)).Id;
            var qf = new QueryFunctions(DbUtil.Db);
            return qf.QuerySql($"{Sql}\n{OrderBy}", tagid);
        }

        public string Results(PictureDirectoryController ctl)
        {
            try
            {
                RegisterHelpers(ctl);
                var compiledTemplate = Handlebars.Compile(Template);
                var s = compiledTemplate(this);
                return s;
            }
            catch (Exception ex)
            {
                return ex.Message + "<br>\n" + ex.StackTrace;
            }
        }

        private void RegisterHelpers(PictureDirectoryController ctl)
        {
            PythonModel.RegisterHelpers(DbUtil.Db);
            Handlebars.RegisterHelper("SmallUrl", (w, ctx, args) =>
            {
                GetPictureUrl(ctx, w, ctx.SmallId,
                    Picture.SmallMissingMaleId, Picture.SmallMissingFemaleId, Picture.SmallMissingGenericId);
            });
            Handlebars.RegisterHelper("MediumUrl", (w, ctx, args) =>
            {
                GetPictureUrl(ctx, w, ctx.MediumId,
                    Picture.MediumMissingMaleId, Picture.MediumMissingFemaleId, Picture.MediumMissingGenericId);
            });
            Handlebars.RegisterHelper("ImagePos", (w, ctx, args) => { w.Write(ctx.X != null || ctx.Y != null ? $"{ctx.X ?? 0}% {ctx.Y ?? 0}%" : "top"); });
            Handlebars.RegisterHelper("IfAccess", (w, opt, ctx, args) =>
            {
                if (HasAccess)
                    opt.Template(w, (object) ctx);
                else
                    opt.Inverse(w, (object) ctx);
            });
            Handlebars.RegisterHelper("PagerTop", (w, ctx, args) => { w.Write(ViewExtensions2.RenderPartialViewToString(ctl, "PagerTop", this)); });
            Handlebars.RegisterHelper("PagerBottom", (w, ctx, args) => { w.Write(ViewExtensions2.RenderPartialViewToString(ctl, "PagerBottom", this)); });
            Handlebars.RegisterHelper("PagerHidden", (w, ctx, args) => { w.Write(ViewExtensions2.RenderPartialViewToString(ctl, "PagerHidden", this)); });
            Handlebars.RegisterHelper("SortBirthday", (w, ctx, args) => { w.Write(SortLink("Birthday")); });
            Handlebars.RegisterHelper("SortName", (w, ctx, args) => { w.Write(SortLink("Name")); });
            Handlebars.RegisterHelper("CityStateZip", (w, ctx, args) => { w.Write(Util.FormatCSZ4(ctx.City, ctx.St, ctx.Zip)); });
            Handlebars.RegisterHelper("BirthDay", (w, ctx, args) =>
            {
                var dob = (string) ctx.DOB;
                w.Write(dob.ToDate().ToString2("m"));
            });
        }

        private static void GetPictureUrl(dynamic ctx, TextWriter w, int? id,
            int missingMaleId, int missingFemailId, int missingGenericId)
        {
            var genderid = ctx.GenderId;
            var created = ctx.PicDate;
            var missingid = genderid == 1 ? missingMaleId : genderid == 2 ? missingFemailId : missingGenericId;
            w.Write($"/Portrait/{id ?? missingid}?v={created?.Ticks ?? 0}");
        }
    }
}