using System;
using System.Linq;
using System.Web.Mvc;

namespace MvcApplication1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult CreateStudent()
        {
            using (var documentSession = MvcApplication.DocumentStore.OpenSession())
            {
                documentSession.Store(new Student { Name = "Patrick", Course = "RavenDB Boot Camp", Date = DateTime.UtcNow });


                documentSession.SaveChanges();
            }

            return RedirectToAction("ListStudents");
        }

        public ActionResult ListStudents()
        {
            using (var documentSession = MvcApplication.DocumentStore.OpenSession())
            {
                var results = documentSession.Query<Student>().ToList();
                return Json(results, JsonRequestBehavior.AllowGet);
            }            
        }
    }

    public class Student
    {
        public string Name { get; set; }
        public string Course { get; set; }
        public DateTime Date { get; set; }
    }
}
