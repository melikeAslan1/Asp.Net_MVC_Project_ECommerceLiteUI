﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ECommerceLiteBLL.Repository;
using ECommerceLiteBLL.Settings;
using ECommerceLiteEntity.Models;
using ECommerceLiteUI.Models;

namespace ECommerceLiteUI.Controllers
{
    public class ProductController : Controller
    {
        CategoryRepo myCategoryRepo = new CategoryRepo();
        ProductRepo myProductRepo=new ProductRepo();
        ProductPictureRepo myProductPictureRepo=new ProductPictureRepo();   

        //Bu controller a Admin gibi yetkili kişiler erişecektir.
        //Burada ürünlerin listelenmesi, ekleme, silme, güncelleme işlemleri yapılacaktır.
        public ActionResult ProductList()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Create()
        {
            //Sayfayı çağırırken ürünün kategorisinin ne olduğu seçmesi lazım bu nedenle sayfaya kategoriler gitmeli

            List<SelectListItem> subCategories = new List<SelectListItem>(); //SelectListItem a Drop Down List derler mvc de.

            //Linq
            //select * from Categories where BaseCategoryId isnot null

            myCategoryRepo.AsQueryable().Where(x => x.BaseCategoryId != null).ToList().ForEach
                (x => subCategories.Add(

                    new SelectListItem()
                    {
                        Text=x.CategoryName,
                        Value=x.Id.ToString()
                    }));

                    ViewBag.SubCategories = subCategories;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public ActionResult Create(ProductViewModel model)
        {
            try
            {
                List<SelectListItem> subCategories = new List<SelectListItem>(); //SelectListItem a Drop Down List derler mvc de.

                //Linq
                //select * from Categories where BaseCategoryId isnot null

                myCategoryRepo.AsQueryable().Where(x => x.BaseCategoryId != null).ToList().ForEach
                    (x => subCategories.Add(

                        new SelectListItem()
                        {
                            Text = x.CategoryName,
                            Value = x.Id.ToString()
                        }));

                ViewBag.SubCategories = subCategories;


                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", "Veri girişleri düzgün olmalıdır");
                    return View(model);
                }

                if(model.CategoryId<=0 || model.CategoryId> myCategoryRepo.GetAll().Count())
                {
                    ModelState.AddModelError("", "Ürüne ait kategori seçilmelidir!");
                    return View(model);
                }

                //Ürün tabloya kayıt olacak. 
                //TODO: Mapleme yapılacak.

                Product product = new Product()
                {
                    ProductName=model.ProductName,
                    Description=model.Description,
                    ProductCode=model.ProductCode,
                    CategoryId=model.CategoryId,
                    Discount=model.Discount,
                    Quantity=model.Quantity,    
                    RegisterDate=DateTime.Now,
                    Price=model.Price
                };

                int insertResult=myProductRepo.Insert(product);
                if(insertResult>0)
                {
                    //Sıfırdan büyükse product tabloya eklendi.
                    //Acaba bu product a resim seçmiş mi? resim seçtiyse o resimlerin yollarını kayıt et.

                    if(model.Files.Any())
                    {

                        ProductPicture productPicture = new ProductPicture();
                        productPicture.ProductId = product.Id;
                        productPicture.RegisterDate = DateTime.Now;
                        int counter = 1;  // bizim sistemde resim adedi 5 olarak belirlendiği için.

                        foreach (var item in model.Files)
                        {
                            if (counter == 5) break;
                            if(item!=null && item.ContentType.Contains("image")&& item.ContentLength>0)
                            {
                                string filename = SiteSettings.StringCharacterConverter(model.ProductName).ToLower().Replace("-", "");

                                string extensionName = Path.GetExtension(item.FileName);

                                string directoryPath = Server.MapPath($"~/ProductPictures/{filename}/{model.ProductCode}");

                                string guid = Guid.NewGuid().ToString().Replace("-", "");

                                string filePath = Server.MapPath($"~/ProductPictures/{filename}/{model.ProductCode}/") + filename + "-"+ counter + "-" + guid + extensionName;

                                if(!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                                item.SaveAs(filePath);
                                //TODO: Buraya birisi çeki düzen verse süper olur
                                if(counter==1)
                                {
                                    productPicture.ProductPicture1= $"/ProductPictures/{filename}/{model.ProductCode}/" +filename + "-" + counter + "-" + guid + extensionName;
                                }
                                if (counter == 2)
                                {
                                    productPicture.ProductPicture2 = $"/ProductPictures/{filename}/{model.ProductCode}/" + filename + "-" + counter + "-" + guid + extensionName;
                                }
                                if (counter == 3)
                                {
                                    productPicture.ProductPicture3 = $"/ProductPictures/{filename}/{model.ProductCode}/" + filename + "-" + counter + "-" + guid + extensionName;
                                }
                            }

                            counter++;
                        }

                        //TODO: YUkarısını for a dönüştürebilir miyiz.
                        //for (int i = 0; i < model.Files.Count; i++)
                        //{

                        //}

                        int productPictureInsertResult=myProductPictureRepo.Insert(productPicture);

                        if(productPictureInsertResult>0)
                        {
                            return RedirectToAction("ProductList", "Product");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Ürün eklendi ama ürüne ait fotoğraf(lar) eklenirken beklenmedik bir hata oluştu! Ürününüzün fotoğraflarını daha sonra" +
                                "tekrar eklemeyi deneyebilirsiniz...");
                            return View(model);
                        }



                    }

                    else
                    {
                        return RedirectToAction("ProductList", "Product");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "HATA: Ürün ekleme işleminde bir hata oluştu! Tekrar Deneyiniz");
                    return View(model);
                }

            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", "Beklenmedik bir hata oluştu");
                // ex loglanacak
                return View(model);
            }
        }
    }
}