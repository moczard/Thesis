using System.Threading.Tasks;
using System.Web.Mvc;
using Thesis.MDM.WebApplication.Models;
using Thesis.MDM.WebApplication.Services;
using System.Collections.Generic;
using System.Linq;
using System;
using Thesis.MDM.WebApp.Services;

namespace Thesis.MDM.WebApplication.Controllers
{
    [Authorize]
    public class PeopleController : Controller
    {          
        public async Task<ActionResult> Index(string searchText)
        {            
            if (searchText != null)
            {
                var result = await SearchService.SearchAsync(searchText);
                var people = result.Results;

                await LogService.SendLogAsync("SEARCH", User.Identity.Name, new { SearchText = searchText });

                return View(people);
            }
            else
            {
                return View();
            }
        }

        [HttpGet]
        public async Task<ActionResult> Suggest(string term, bool fuzzy = true)
        {
            var results = await SearchService.SuggestAsync(term, fuzzy);
            List<string> suggestions = new List<string>();
            foreach (var result in results.Results)
            {
                suggestions.Add(result.Text);
            }

            List<string> uniqueItems = suggestions.Distinct().ToList();

            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = uniqueItems
            };
        }    

        public ActionResult Create()
        {
            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "FirstName,LastName,Email,Gender,City,Country,StreetAddress,CompanyName,JobTitle,PhoneNumber")] Person person)
        {
            person.Id = Guid.NewGuid().ToString();
            var currentUser = User.Identity.Name;
            await EventService.SendUpdateAsync(person, currentUser);
            await LogService.SendLogAsync("CREATE", User.Identity.Name);

            return RedirectToAction("Index", "people");
        }
        
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var person = await SearchService.GetAsync(id);
            
            if (person == null)
            {
                return HttpNotFound();
            }
            return View(person);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,FirstName,LastName,Email,Gender,City,Country,StreetAddress,CompanyName,JobTitle,PhoneNumber")] Person person)
        {
            var currentUser = User.Identity.Name;
            await EventService.SendUpdateAsync(person, currentUser);
            await LogService.SendLogAsync("EDIT", User.Identity.Name);
            
            return RedirectToAction("Index", "people");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Merge(string id1, string id2)
        {
            var person1 = await SearchService.GetAsync(id1);
            var person2 = await SearchService.GetAsync(id2);          

            var mergedPerson = new Person
            {
                Id = id1 + "-" + id2,
                FirstName = $"[{person1.FirstName}],[{person2.FirstName}]",
                LastName = $"[{person1.LastName}],[{person2.LastName}]",
                Email = $"[{person1.Email}],[{person2.Email}]",
                Gender = $"[{person1.Gender}],[{person2.Gender}]",
                City = $"[{person1.City}],[{person2.City}]",
                Country = $"[{person1.Country}],[{person2.Country}]",
                StreetAddress =$"[{person1.StreetAddress}],[{person2.StreetAddress}]",
                CompanyName = $"[{person1.CompanyName}],[{person2.CompanyName}]",
                JobTitle = $"[{person1.JobTitle}],[{person2.JobTitle}]",
                PhoneNumber = $"[{person1.PhoneNumber}],[{person2.PhoneNumber}]"
            };

            var currentUser = User.Identity.Name;

            await EventService.SendMergeAsync(person1, person2, mergedPerson, currentUser, false);
           
            await LogService.SendLogAsync("MERGE", User.Identity.Name);

            return RedirectToAction("Index", "people");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Unmerge(string id)
        {
            var mergedPerson = await SearchService.GetAsync(id);            

            var person1 = new Person
            {
                Id = id.Split('-')[0],
                FirstName = mergedPerson.FirstName.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0,1),
                LastName = mergedPerson.LastName.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                Email = mergedPerson.Email.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                Gender = mergedPerson.Gender.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                City = mergedPerson.City.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                Country = mergedPerson.Country.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                StreetAddress = mergedPerson.StreetAddress.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                CompanyName = mergedPerson.CompanyName.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                JobTitle = mergedPerson.JobTitle.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
                PhoneNumber = mergedPerson.PhoneNumber.Split(new string[] { "],[" }, StringSplitOptions.None)[0].Remove(0, 1),
            };

            var person2 = new Person
            {
                Id = id.Split('-')[1],
                FirstName = mergedPerson.FirstName.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                LastName = mergedPerson.LastName.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                Email = mergedPerson.Email.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                Gender = mergedPerson.Gender.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                City = mergedPerson.City.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                Country = mergedPerson.Country.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                StreetAddress = mergedPerson.StreetAddress.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                CompanyName = mergedPerson.CompanyName.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                JobTitle = mergedPerson.JobTitle.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", ""),
                PhoneNumber = mergedPerson.PhoneNumber.Split(new string[] { "],[" }, StringSplitOptions.None)[1].Replace("]", "")
            };

            var currentUser = User.Identity.Name;

            await EventService.SendMergeAsync(person1, person2, mergedPerson, currentUser, true);
            await LogService.SendLogAsync("UNMERGE", User.Identity.Name);

            return RedirectToAction("Index", "people");
        }
    }
}
