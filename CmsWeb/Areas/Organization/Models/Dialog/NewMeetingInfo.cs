using System;
using System.ComponentModel;
using CmsWeb.Code;

namespace CmsWeb.Areas.Org2.Dialog.Models
{
    public class NewMeetingInfo
    {
        [DisplayName("Choose A Schedule")]
        public CodeInfo Schedule { get; set; }
        public CodeInfo AttendCredit { get; set; }
        [DateAndTimeValid]
        public DateTime MeetingDate { get; set; }
        public bool ByGroup { get; set; }
        public string GroupFilterPrefix { get; set; }
        public string HighlightGroup { get; set; }
        public bool UseAltNames { get; set; }
        public int? OrganizationId { get; set; }
    }
}