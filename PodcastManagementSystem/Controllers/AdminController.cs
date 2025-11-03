using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PodcastManagementSystem.Data;
using PodcastManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PodcastManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin
        public IActionResult Dashboard()
        {
            return View();
        }


        /////////////////////////////////////////////////////////
        /// UserManagement
        /////////////////////////////////////////////////////////
        //Admin UserManagement Dashboard Routing
        public async Task<IActionResult> UserManagement()
        {
            return View(await _context.Users.ToListAsync());
        }

        // Routing - GET: Admin/Edit/5
        public async Task<IActionResult> UserEdit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users.FindAsync(id);
            if (applicationUser == null)
            {
                return NotFound();
            }
            return View(applicationUser);
        }

        // CRUD - POST: Admin/UserEdit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(Guid id, [Bind("Id,Role,UserName,Email,PhoneNumber")] ApplicationUser formUser)
        {
            if (id != formUser.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(formUser);

            var userInDb = await _context.Users.FindAsync(id);
            if (userInDb == null)
                return NotFound();

            try
            {
                // Update allowed fields only
                userInDb.Role = formUser.Role;
                userInDb.UserName = formUser.UserName;
                userInDb.Email = formUser.Email;
                userInDb.PhoneNumber = formUser.PhoneNumber;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(UserManagement));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Another admin modified this user. Please refresh and try again.");
                return View(formUser);
            }
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UserEdit(Guid id, [Bind("Role,Email,UserName,PhoneNumber")] ApplicationUser formUser)
        //{
        //    if (id != formUser.Id)
        //        return NotFound();

        //    if (!ModelState.IsValid)
        //        return View(formUser);

        //    var userInDb = await _context.Users.FindAsync(id);

        //    if (userInDb == null)
        //        return NotFound();

        //    try
        //    {
        //        // Update only the fields you allow admins to edit
        //        userInDb.Role = formUser.Role;
        //        userInDb.Email = formUser.Email;
        //        userInDb.UserName = formUser.UserName;
        //        userInDb.PhoneNumber = formUser.PhoneNumber;

        //        // Save the changes — EF is tracking userInDb
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(UserManagement));
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        ModelState.AddModelError("", "Another admin modified this user. Please refresh and try again.");
        //        return View(formUser);
        //    }
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UserEdit(Guid id, [Bind("Role,Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount")] ApplicationUser applicationUser)
        //{
        //    if (id != applicationUser.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(applicationUser);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!ApplicationUserExists(applicationUser.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(UserManagement));
        //    }
        //    return View(applicationUser);
        //}

        // Routing - GET: Admin/Details/5
        public async Task<IActionResult> UserDetails(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }

            return View(applicationUser);
        }

        // Routing - GET: Admin/Delete/5
        public async Task<IActionResult> UserDelete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var applicationUser = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (applicationUser == null)
            {
                return NotFound();
            }

            return View(applicationUser);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            //var applicationUser = await _context.Users.FindAsync(id);
            //if (applicationUser != null)
            //{
            //    _context.Users.Remove(applicationUser);
            //}

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(UserManagement));
            }

            //await _context.SaveChangesAsync();
            //return RedirectToAction(nameof(UserManagement));
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(user);

        }


        // Routing - GET: Admin/Create
        [HttpGet]
        public IActionResult UserCreate()
        {
            return View();
        }

        // POST: Admin/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate(ApplicationUser model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Optionally add role
                    await _userManager.AddToRoleAsync(user, model.Role.ToString());
                    return RedirectToAction("UserManagement");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UserCreate([Bind("Role,UserName,Email,PhoneNumber")] ApplicationUser applicationUser, string password)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        applicationUser.Id = Guid.NewGuid();

        //        // Use the UserManager to create the user securely
        //        var result = await _userManager.CreateAsync(applicationUser, password);

        //        if (result.Succeeded)
        //        {
        //            var ROOOL = applicationUser.Role;
        //            // Optional: assign the role if you're using Identity roles
        //            await _userManager.AddToRoleAsync(applicationUser, "");

        //            return RedirectToAction(nameof(UserManagement));
        //        }

        //        // Add any identity errors to the model state
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError("", error.Description);
        //        }
        //    }

        //    return View(applicationUser);
        //}


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> UserCreate([Bind("Role,Id,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnd,LockoutEnabled,AccessFailedCount")] ApplicationUser applicationUser)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        applicationUser.Id = Guid.NewGuid();
        //        _context.Add(applicationUser);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(UserManagement));
        //    }
        //    return View(applicationUser);
        //}



        /////////////////////////////////////////////////////////
        /// episodeCreationApprovalQueue
        /////////////////////////////////////////////////////////


        /////////////////////////////////////////////////////////
        /// analyticsDashboard
        /////////////////////////////////////////////////////////













        private bool ApplicationUserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
