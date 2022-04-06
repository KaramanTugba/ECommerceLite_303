﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Account;
using ECommerceLiteBLL.Repository;
using ECommerceLiteBLL.Settings;
using ECommerceLiteEntity.IdentityModels;
using ECommerceLiteEntity.Models;
using ECommerceLiteUI.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using ECommerceLiteEntity.Enums;
using System.Threading.Tasks;
using ECommerceLiteEntity.ViewModels;

namespace ECommerceLiteUI.Controllers
{
    public class AccountController : BaseController
    {
        //Global alan
        //Not: Bir sonraki projede repoları UI nin içinde new lemeyeceğiz.
        //Çünkü bu bağımlılık oluşturur!bir sonraki projede bağımlılıkları
        //tersine çevirme işlemi olarak bilinen Dependency Injection işlemleri yapacağız.

        CustomerRepo myCustomerRepo = new CustomerRepo();
        PassiveUserRepo myPassiveUserRepo = new PassiveUserRepo();
        UserManager<ApplicationUser> myUserManager = MembershipTools.NewUserManager();
        UserStore<ApplicationUser> myUserStore = MembershipTools.NewUserStore();
        RoleManager<ApplicationRole> myRoleManager = MembershipTools.NewRoleManager();

        [HttpGet]
        public ActionResult Register()
        {
            //kayıtol sayfası
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]//Bot hesaplarını engeller
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)//model validasyonları sağladı mı?
                {
                    return View(model);
                }
                var checkUserTC = myUserStore.Context.Set<Customer>().FirstOrDefault(x => x.TCNumber == model.TCNumber)?.TCNumber;
                if (checkUserTC != null)//Buldu
                {
                    ModelState.AddModelError("", "Bu TC kimlik numarası ile sisteme kayıt yapılmıştır.");
                    return View(model);
                }
                var checkUserEmail = myUserStore.Context.Set<ApplicationUser>().FirstOrDefault(x => x.Email == model.Email)?.Email;
                if (checkUserEmail != null)//Buldu
                {
                    ModelState.AddModelError("", "Bu email ile sisteme kayıt yapılmıştır.");
                    return View(model);
                }
                //aktivasyon kodu üretelim

                var activationCode = Guid.NewGuid().ToString().Replace("-", "");
                // artık sisteme kayıt olabilir

                var newUser = new ApplicationUser()
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    Email = model.Email,
                    UserName = model.TCNumber,
                    ActivationCode=activationCode
                };

                
                //ekleyeceğiz
                var createResult = myUserManager.CreateAsync(newUser, model.Password);

                //todo: createResult.Isfault ne acaba
                if (createResult.Result.Succeeded)
                {
                    //görev başarıyla tamamlandıysa kişi aspnetusers tablosuna eklenmiştir.
                    //yeni kayıt olduğu için bu kişiye pasif rol verilecektir.
                    //Kişi emailine gelen aktivasyon koduna tıklarsa pasifiklikten çıkıp customer olabilir.

                    await myUserManager.AddToRoleAsync(newUser.Id, Roles.Passive.ToString());
                    PassiveUser myPassiveUser = new PassiveUser()
                    {
                        UserId = newUser.Id,
                        TCNumber = model.TCNumber,
                        IsDeleted = false,
                        LastActiveTime = DateTime.Now
                    };
                    //  myPassiveUserRepo.Insert(myPassiveUser)
                    await myPassiveUserRepo.InsertAsync(myPassiveUser);
                    //email gönderilecek
                    //site adresi alıyoruz.
                    var siteURL = Request.Url.Scheme + Uri.SchemeDelimiter
                        + Request.Url.Host +
                        (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port);
                    await SiteSettings.SendMail(new MailModel()
                    {
                        To = newUser.Email,
                        Subject = "ECommerceLite Site Aktivasyon Emaili",
                        Message = $"Merhaba {newUser.Name} {newUser.Surname}," +
                        $"<br/>Hesabınızı aktifleştirmek için <b>" +
                        $"<a href='{siteURL}/Account/Activation?" +
                        $"code={activationCode}'>Aktivasyon Linkine</a></b> tıklayınız..."
                    });
                    // işlemler bitti...
                    return RedirectToAction("Login", "Account", new { email = $"{newUser.Email}" });
                }
                else
                {
                    ModelState.AddModelError("", "Kayıt işleminde beklenmedik bir hata oluştu.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                //Todo: loglama yapılacak
                ModelState.AddModelError("", "Bekenmedik bir hata oluştu.Tekrar Deneyiniz");
                return View(model);

            }
        }

        [HttpGet]
        public async Task<ActionResult> Activation(string code)
        {//select * from aspnuser where activationcode='dfghjklşid'
            try
            {
                var user = myUserStore.Context.Set<ApplicationUser>().FirstOrDefault(x => x.ActivationCode == code);
                if (user==null)
                {
                    ViewBag.ActivationResult = "Aktivasyon işlemi başarısız.Sistem yöneticisinden yeniden email isteyiniz.";
                    return View();
                }
                //user bulundu.Yukarıda takılmadıysa.
                if (user.EmailConfirmed)//zaten aktifleşmiş mi?
                {
                    ViewBag.ActivationResult = "Aktivasyon işleminiz zaten gerçekleşmiştir.Giriş yaparak sistemi kullanabilirsiniz";
                    return View();
                }
                user.EmailConfirmed = true;
                await myUserStore.UpdateAsync(user);
                await myUserStore.Context.SaveChangesAsync();
                //User artık aktif
                PassiveUser passiveUser = myPassiveUserRepo.AsQueryable().FirstOrDefault(x => x.UserId == user.Id);
                if (passiveUser!=null)
                {
                    //todo:PassiveUser tablosuna TargetRole ekleme işlemini daha sonra yapalım. Kafalarındaki soru işareti gittikten sonra...

                    passiveUser.IsDeleted = true;
                    myPassiveUserRepo.Update();

                    Customer customer = new Customer()
                    {
                        UserId = user.Id,
                        TCNumber = passiveUser.TCNumber,
                        IsDeleted = true,
                        LastActiveTime = DateTime.Now
                    };
                    await myCustomerRepo.InsertAsync(customer);

                    //aspnetuser tablosunda da bu kişinin artık customer mertebesine ulaştığını bildirelim.
                    myUserManager.RemoveFromRole(user.Id, Roles.Passive.ToString());
                    myUserManager.AddToRole(user.Id, Roles.Customer.ToString());
                    //işlemin başarılı olduğuna dair mesajı gönderelim

                    ViewBag.ActivationResult = $"Merhaba Sayın {user.Name}{user.Surname}, aktifleştirme işleminiz başarılıdır.Giriş yapıp sistemikullanabilirsiniz.";
                    return View();
                }

                //not: beyin fırtınası yapılacak.
                // Passive user null gelirse nasıl bir yol izlenir? passiveuser null gelmesi çok büyük bir problem mi?
                //Customer da bu kişi kayıtlı mı? Customer a kayıtlı ise problem yok. Kayıtlı değilse problem yok.
                return View();
            }
            catch (Exception ex)
            {
                //todo: loglama yapılacak.
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu.");
                return View();
            }
        }

        [HttpGet]
        [Authorize]//login olmadan buraya girmemesi için ( yetkin olmalı )
        public ActionResult UserProfile()
        {
            //login olan kişinin id biligisini al
            var user = myUserManager.FindById(HttpContext.User.Identity.GetUserId());
            if (user!=null)
            {
                ProfileViewModel model = new ProfileViewModel()
                {
                    Name = user.Name,
                    Surname=user.Surname,
                    Email=user.Email,
                    TCNumber=user.UserName
                };
                return View(model);
            }
            //user null ise
            ModelState.AddModelError("", "Beklenmedik bir sorun oluşmuş olabilir mi? Giriş yapıp, tekrar deneyiniz");
            return View();

            // kişiyi bulacağız ve mevcut bilgilerini profileviewmodele atayıp sayfaya göndereceğiz.
        }


        [HttpGet]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UserProfile(ProfileViewModel model)
        {
            try
            {
                //sisteme kayıt olmuş, giriş yapmış kişi Hesabıma tıkladı.
                //Bilgilerini gördü.Bilgilerinde değişiklik yaptı. Biz burada kontrol edeceğiz. Yapılan dedğişiklikleri tespit edip db i güncelleştireceğiz.

                var user = myUserManager.FindById(HttpContext.User.Identity.GetUserId());
                if (user==null)
                {
                    ModelState.AddModelError("", "Mevcut kullanıcı bilgilerinize ulaşılamadığı için işlem yapamıyoruz.");
                    return View(model);
                }
                //bir user herhangi bir bilgisini değiştirecekse Parolasını girmek zorunda.
                //Bu nedenle model ile gelen parola db deki parola ile eşleşiyor mu diye bakmak lazım...
                if (myUserManager.PasswordHasher.VerifyHashedPassword(user.PasswordHash,model.Password)==PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Mevcut şifrenizi yanlış girdiğiniz için bilgilerinizi güncelleyemedik.Lütfen tekrar deneyiniz.");
                    return View(model);
                }
                //başarılıysa yani parolayı doğru yazdı.//bilgileri güncelleyeceğiz

                user.Name = model.Name;
                user.Surname = model.Surname;
                await myUserManager.UpdateAsync(user);
                ViewBag.Result = "Bilgileriniz güncellendi";
                var updatedModel = new ProfileViewModel()
                {
                    Name = user.Name,
                    Surname = user.Surname,
                    TCNumber = user.UserName,
                    Email = user.Email
                };
                return View(updatedModel);


            }
            catch (Exception ex)
            {
                //ex loglanacak
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu! Tekrar deneyiniz");
                return View(model);
            }
        }

    }
}