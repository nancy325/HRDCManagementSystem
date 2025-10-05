using System.ComponentModel.DataAnnotations;

namespace HRDCManagementSystem.Models.ViewModels
{
    public class HelpQueryViewModel
    {
        public int HelpQueryID { get; set; }
        public int EmployeeSysID { get; set; }

        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Query Type")]
        public string QueryType { get; set; }

        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Display(Name = "Message")]
        public string Message { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Viewed")]
        public bool ViewedByAdmin { get; set; }

        [Display(Name = "Resolved Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? ResolvedDate { get; set; }

        [Display(Name = "Submission Date")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? CreateDateTime { get; set; }
    }
}