﻿using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager )
        {
            _db = db;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult RoleManagment(string userId)
        {
            string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;
            RoleManagmentVM RoleVm = new RoleManagmentVM()
            {
                ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                RoleList = _db.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _db.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),


            };
            RoleVm.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;
            return View(RoleVm);
        }
        [HttpPost]
        public IActionResult RoleManagment(RoleManagmentVM roleManagmentVM)
        {
            string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == roleManagmentVM.ApplicationUser.Id).RoleId;
            string oldRole = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;
            if(!(roleManagmentVM.ApplicationUser.Role == oldRole))
            {
                ApplicationUser applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleManagmentVM.ApplicationUser.Id);
                if(roleManagmentVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleManagmentVM.ApplicationUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _db.SaveChanges();

                _userManager.RemoveFromRoleAsync(applicationUser,oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, roleManagmentVM.ApplicationUser.Role).GetAwaiter().GetResult();

            }
            return RedirectToAction(nameof(Index));
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUserList = _db.ApplicationUsers.Include(x => x.Company).ToList();
            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach (var user in objUserList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;

                if(user.Company == null)
                {
                    user.Company = new() { Name = " " };
                }
            }
            return Json(new { data = objUserList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {
            var objFromdb =_db.ApplicationUsers.FirstOrDefault(x => x.Id == id);
            if (objFromdb == null) 
            {
                return Json(new { success = false, message = "Error while  Locking/Unlocking Successfully" });
            }
            if (objFromdb.LockoutEnd != null  && objFromdb.LockoutEnd > DateTime.Now)
            {
                //user currenlty locked and we need to unlock them
                objFromdb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromdb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _db.SaveChanges();  
            return Json(new { success = true, message = "Operation Successfully" });
        }
    }
}
#endregion
