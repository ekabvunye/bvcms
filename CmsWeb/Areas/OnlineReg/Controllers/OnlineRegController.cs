using System;
using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsWeb.Models;
using UtilityExtensions;
using CmsWeb.Areas.Manage.Controllers;
using CmsWeb.Areas.OnlineReg.Models.Payments;
using System.Text;

namespace CmsWeb.Areas.OnlineReg.Controllers
{
    [ValidateInput(false)]
    public partial class OnlineRegController : CmsController
    {
#if DEBUG
        private const int INT_timeout = 1600000;
#else
        private const int INT_timeout = 60000;
#endif

        // Main page
        public ActionResult Index(int? id, int? div, bool? testing, int? o, int? d, string email, bool? nologin)
        {
            if (!id.HasValue && !div.HasValue)
                return Content("no organization");
            var m = new OnlineRegModel
            {
                divid = div,
                orgid = id,
            };
            if (m.org == null && m.div == null)
                return Content("invalid registration");

            if (m.org != null)
            {
                if ((m.org.RegistrationTypeId ?? 0) == (int)CmsData.Organization.RegistrationEnum.None)
                    return Content("no registration allowed on this org");
            }
            else if (m.div != null)
                if (OnlineRegModel.UserSelectClasses(m.divid).Count() == 0)
                    return Content("no registration allowed on this div");
            m.URL = Request.Url.OriginalString;

            Session["gobackurl"] = m.URL;

            ViewData["timeout"] = INT_timeout;
            SetHeaders(m.divid ?? m.orgid ?? 0);

#if DEBUG

            m.testing = true;
            m.username = "David";
            m.password = "toby123";
#else
            m.testing = testing;
#endif
            if (Util.ValidEmail(email))
            {
                m.nologin = true;
                m.CreateList();
                m.List[0].email = email;
            }
            else if (nologin == true)
            {
                m.nologin = true;
                m.CreateList();
            }
            return View(m);
        }
        // authenticate user
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Login(OnlineRegModel m)
        {
            var ret = AccountController.AuthenticateLogon(m.username, m.password, Session, Request);
            if (ret is string)
            {
                ModelState.AddModelError("authentication", ret.ToString());
                return View("Flow/List", m);
            }
            var user = ret as User;
            if (m.orgid == Util.CreateAccountCode)
                return Content("/Person/Index/" + Util.UserPeopleId);

            m.CreateList();
            m.UserPeopleId = user.PeopleId;
            if (m.ManagingSubscriptions())
            {
                TempData["ms"] = Util.UserPeopleId;
                return Content("/OnlineReg/ManageSubscriptions/{0}".Fmt(m.divid));
            }
            m.List[0].LoggedIn = true;
            return View("Flow/List", m);
        }
        // Register without logging in
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult NoLogin(OnlineRegModel m)
        {
            m.nologin = true;
            m.CreateList();
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Register(int id, OnlineRegModel m)
        {
            if (m.classid.HasValue)
                m.orgid = m.classid;
            var p = m.LoadExistingPerson(id);
            p.ValidateModelForFind(ModelState, m);
            if (!ModelState.IsValid)
                return View("Flow/List", m);
            m.List[m.List.Count - 1] = p;
            if (p.ManageSubscriptions() && p.Found == true)
            {
                p.OtherOK = true;
                return View("Flow/List", m);
            }
            if (p.org != null && p.Found == true)
            {
                p.IsFilled = p.org.OrganizationMembers.Count() >= p.org.Limit;
                if (p.IsFilled)
                    ModelState.AddModelError(p.ErrorTarget, "Sorry, but registration is closed.");
                if (p.Found == true)
                    p.FillPriorInfo();
                if (!p.AnyOtherInfo())
                    p.OtherOK = true;
                return View("Flow/List", m);
            }
            if (p.ShowDisplay() && p.org != null && p.ComputesOrganizationByAge())
                p.classid = p.org.OrganizationId;
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Cancel(int id, OnlineRegModel m)
        {
            m.List.RemoveAt(id);
            if (m.List.Count == 0)
                m.List.Add(new OnlineRegPersonModel
                {
                    divid = m.divid,
                    orgid = m.orgid,
                    LoggedIn = m.UserPeopleId.HasValue,
                });
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ShowMoreInfo(int id, OnlineRegModel m)
        {
            DbUtil.Db.SetNoLock();
            var p = m.List[id];
            p.index = id;
            p.ValidateModelForFind(ModelState, m);
            if (p.org != null && p.Found == true)
            {
                p.IsFilled = p.org.OrganizationMembers.Count() >= p.org.Limit;
                if (p.IsFilled)
                    ModelState.AddModelError(p.inputname("dob"), "Sorry, but registration is closed.");
                if (p.Found == true)
                    p.FillPriorInfo();
                if (!p.AnyOtherInfo())
                    p.OtherOK = true;
                return View("Flow/List", m);
            }
            if (!p.whatfamily.HasValue && (id > 0 || p.LoggedIn == true))
            {
                ModelState.AddModelError(p.inputname("whatfamily"), "Choose a family option");
                return View("Flow/List", m);
            }
            switch (p.whatfamily)
            {
                case 1:
                    var u = DbUtil.Db.LoadPersonById(m.UserPeopleId.Value);
                    p.address = u.PrimaryAddress;
                    p.city = u.PrimaryCity;
                    p.state = u.PrimaryState;
                    p.zip = u.PrimaryZip.FmtZip();
                    break;
                case 2:
                    var pb = m.List[id - 1];
                    p.address = pb.address;
                    p.city = pb.city;
                    p.state = pb.state;
                    p.zip = pb.zip;
                    break;
                default:
#if DEBUG
                    //p.address = "235 Riveredge Cv.";
                    //p.city = "Cordova";
                    //p.state = "TN";
                    //p.zip = "38018";
                    //p.gender = 1;
                    //p.married = 10;
#endif
                    break;
            }
            p.ShowAddress = true;
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult PersonFind(int id, OnlineRegModel m)
        {
            DbUtil.Db.SetNoLock();
            var p = m.List[id];
            p.index = id;
            if (m.classid.HasValue)
            {
                m.orgid = m.classid;
                p.classid = m.classid;
                p.orgid = m.classid;
            }
            p.classid = m.classid;
            p.PeopleId = null;
            p.ValidateModelForFind(ModelState, m);
            if (p.ManageSubscriptions())
            {
                p.OtherOK = true;
                //if (p.Found == true)
                //    return Content("/OnlineReg//{0}".Fmt(m.divid));
            }
            else if (p.org != null)
            {
                p.IsFilled = p.org.OrganizationMembers.Count() >= p.org.Limit;
                if (p.IsFilled)
                    ModelState.AddModelError(p.inputname("dob"), "Sorry, but registration is closed.");
                if (p.Found == true)
                    p.FillPriorInfo();
                if (!p.AnyOtherInfo())
                    p.OtherOK = true;
            }
            if (p.ShowDisplay() && p.ComputesOrganizationByAge())
                p.classid = p.org.OrganizationId;
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SubmitNew(int id, OnlineRegModel m)
        {
            var p = m.List[id];
            p.index = id;
            p.ValidateModelForNew(ModelState);

            if (ModelState.IsValid)
            {
                if (m.ManagingSubscriptions())
                {
                    p.IsNew = true;
                    m.ConfirmManageSubscriptions();
                    ViewData["ManagingSubscriptions"] = true;
                    ViewData["CreatedAccount"] = m.List[0].CreatingAccount;
                    DbUtil.Db.SubmitChanges();
                    ViewData["email"] = m.List[0].person.EmailAddress;
                    ViewData["orgname"] = m.div.Name;
                    ViewData["URL"] = m.URL;
                    ViewData["timeout"] = INT_timeout;
                    return View("ConfirmManageSub");
                }
                if (p.org == null && p.ComputesOrganizationByAge())
                    ModelState.AddModelError(p.ErrorTarget, "Sorry, cannot find an appropriate age group");
                else if (!p.ManageSubscriptions())
                {
                    p.IsFilled = p.org.OrganizationMembers.Count() >= p.org.Limit;
                    if (p.IsFilled)
                        ModelState.AddModelError(p.inputname("dob"), "Sorry, that age group is filled");
                }
                p.IsNew = true;
            }
            p.IsValidForExisting = ModelState.IsValid == false;
            if (p.IsNew)
                p.FillPriorInfo();
            if (p.ShowDisplay() && p.ComputesOrganizationByAge())
                p.classid = p.org.OrganizationId;
            if (!p.AnyOtherInfo())
                p.OtherOK = ModelState.IsValid;
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SubmitOtherInfo(int id, OnlineRegModel m)
        {
            var p = m.List[id];
            p.index = id;
            p.ValidateModelForOther(ModelState);
            p.OtherOK = ModelState.IsValid;
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult AddAnotherPerson(OnlineRegModel m)
        {
            if (!ModelState.IsValid)
                return View("Flow/List", m.List);
#if DEBUG
            m.List.Add(new OnlineRegPersonModel
            {
                divid = m.divid,
                orgid = m.orgid,
                first = "Bethany",
                last = "Carroll",
                //bmon = 1,
                //bday = 29,
                //byear = 1987,
                dob = "1/29/87",
                email = "davcar@pobox.com",
                phone = "9017581862".FmtFone(),
                LoggedIn = m.UserPeopleId.HasValue,
            });
#else
            m.List.Add(new OnlineRegPersonModel
            {
                divid = m.divid,
                orgid = m.orgid,
                LoggedIn = m.UserPeopleId.HasValue,
            });
#endif
            return View("Flow/List", m);
        }
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CompleteRegistration(OnlineRegModel m)
        {
            DbUtil.Db.SetNoLock();

            var d = new ExtraDatum { Stamp = Util.Now };
            DbUtil.Db.ExtraDatas.InsertOnSubmit(d);
            DbUtil.Db.SubmitChanges();

            var p = m.List[0];
            var pp = p.person;
            if (m.user != null)
                pp = m.user;
            var ti = new Transaction
            {
                Name = m.NameOnAccount,
                Address = pp != null ? pp.PrimaryAddress : p.address,
                Amt = m.Amount(),
                Amtdue = m.TotalAmountDue(),
                City = pp != null ? pp.PrimaryCity : p.city,
                Emails = pp != null ? pp.EmailAddress : p.city,
                Phone = (pp != null ? Util.PickFirst(pp.HomePhone, pp.CellPhone) : p.phone).FmtFone(),
                State = pp != null ? pp.PrimaryState : p.state,
                Zip = pp != null ? pp.PrimaryZip : p.zip,
                Testing = m.testing ?? false,
                Description = m.Header,
                OrgId = m.orgid,
                Url = m.URL,
                DatumId = d.Id,
            };
            ti.TransactionGateway = DbUtil.Db.Setting("TransactionGateway", "ServiceU");
            DbUtil.Db.Transactions.InsertOnSubmit(ti);
            DbUtil.Db.SubmitChanges();
            m.TranId = ti.Id;

            d.Data = Util.Serialize<OnlineRegModel>(m);
            DbUtil.Db.SubmitChanges();

            if (m.Amount() == 0 && !m.Terms.HasValue())
                return RedirectToAction("Confirm",
                    new
                    {
                        id = d.Id,
                        TransactionID = "zero due",
                    });
            if (m.ChoosingSlots())
                return RedirectToAction("PickSlots", new { id = d.Id });

            var terms = Util.PickFirst(m.Terms, "");
            if (terms.HasValue())
                ViewData["Terms"] = terms;
            ViewData["timeout"] = INT_timeout;

            SetHeaders(m.orgid ?? m.divid ?? 0);
            if (m.Amount() == 0 && m.Terms.HasValue())
                return View("Terms", d.Id);
            if (ti.TransactionGateway == "ServiceU")
                if (DbUtil.Db.Setting("newcoupon", "false") == "true")
                    return View("NewPayment", m);
                else
                    return View("Payment", ti);

            var pf = new PaymentForm { ti = ti };
                if (DbUtil.Db.Setting("newcoupon", "false") == "true")
                    return View("NewProcessPayment", pf);
                else
                    return View("ProcessPayment", pf);
        }
    }
}
