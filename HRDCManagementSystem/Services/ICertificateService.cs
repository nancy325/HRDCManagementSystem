using System.Collections.Generic;
using HRDCManagementSystem.Models;

namespace HRDCManagementSystem.Services
{
   
    // Interface for DI
    public interface ICertificateService
    {
        List<Certificate> GetCertificates();
        Certificate? GetCertificateById(int id);
    }

}
