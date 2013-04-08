using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using System.Globalization;
using Sitecore.Data;
using Sitecore.Data.Databases;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using Sitecore.SecurityModel;

namespace IdentityTheft911.jobs
{
    public partial class BraftonImport : System.Web.UI.Page
    {
        string strArticleType = "";
        string strType = "";
        string strTopic = "";
        string strTitle = "";
        string strDescription = "";
        string strPubDate = "";
        string strMainContent = "";
        string strBraftonID = "";
        string strSmallImg = "";
        string strLargeImg = "";
        string strAltText = "";
        string strCategoryID = "";
        string strCategory = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            XDocument xDoc = XDocument.Load("http://feeds.brafton.com/?dfa34caa-f558-48d6-af7c-3b0ea55c46eb");
            
            var newsalerts = (from xeItem in xDoc.Element("InfoStreamResults").Elements("Article")
                              let strTitle = xeItem.Descendants("Heading").Count() > 0 ? xeItem.Element("Heading").Value.Trim() : null
                              let strMainContent = xeItem.Descendants("Contents").Count() > 0 ? xeItem.Element("Contents").Value.Trim() : null
                              let dtPubDate = xeItem.Descendants("Date").Count() > 0 ? DateTime.Parse(xeItem.Element("Date").Value, new CultureInfo("en-GB", false)) : DateTime.MinValue
                              let strDescription = xeItem.Descendants("Summary").Count() > 0 ? xeItem.Element("Summary").Value.Trim() : null
                              let strBraftonID = xeItem.Attribute("ID").Value.ToString()
                              let strSmallImg = xeItem.Descendants("Picture").Count() > 0 ? xeItem.Element("Picture").Element("Small").Value : null
                              let strLargeImg = xeItem.Descendants("Picture").Count() > 0 ? xeItem.Element("Picture").Element("Large").Value : null
                              let strAltText = xeItem.Descendants("Picture").Count() > 0 ? xeItem.Element("Picture").Element("PhotoTag").Value : null
                              where dtPubDate > DateTime.MinValue 
                              orderby dtPubDate ascending
                              select new
                              {
                                  BraftonID = strBraftonID,
                                  Title = strTitle,
                                  MainContent = strMainContent,
                                  PubDate = dtPubDate,
                                  Description = strDescription,
                                  SmallImg = strSmallImg,
                                  LargeImg = strLargeImg,
                                  AltText = strAltText,
                                  Categories = xeItem.Elements("Categories")
                              }).ToList();

            if (newsalerts.Count() > 0)
            {
                foreach (var newsalert in newsalerts)
                {
                    strArticleType = "";
                    strCategory = "";
                    strCategoryID = "";
                    strTopic = "";
                    strType = "";

                    // The code below handles articles that have multi-tagging on the category.
                    foreach (var category in newsalert.Categories)
                    {
                        foreach (var c in category.Elements("Category"))
                        {
                            if (c.Value == "Weekly Feature Article")
                            {
                                // Indicate that a new Weekly Feature Article was found.
                                strArticleType = "WFA";    // abbreviaton for Weekly Feature Article
                                strType = "FeatureArticle";

                                // Handle the XML layout without multi-tagging on the category.
                                if (newsalert.Categories.Elements("Category").Count() == 1) 
                                {
                                    strTopic = c.Value.Replace(" ", "");
                                    strTopic = Database.GetDatabase("master").SelectSingleItem("/sitecore/content/GlobalElements/KnowledgeCenter/Topics/" + strTopic) != null
                                               ? Database.GetDatabase("master").SelectSingleItem("/sitecore/content/GlobalElements/KnowledgeCenter/Topics/" + strTopic).ID.ToString()
                                               : "";
                                    strCategory = c.Value;
                                    strCategoryID = c.Attribute("ID").Value.ToString();
                                }
                            }
                            else
                            {
                                strTopic = c.Value.Replace(" ", "");
                                strTopic = Database.GetDatabase("master").SelectSingleItem("/sitecore/content/GlobalElements/KnowledgeCenter/Topics/" + strTopic) != null
                                           ? Database.GetDatabase("master").SelectSingleItem("/sitecore/content/GlobalElements/KnowledgeCenter/Topics/" + strTopic).ID.ToString()
                                           : "";
                                strCategory = c.Value;
                                strCategoryID = c.Attribute("ID").Value.ToString();
                            }
                        }
                    }
                    
                    strTitle = newsalert.Title;
                    strDescription = newsalert.Description;
                    strPubDate = Sitecore.DateUtil.ToIsoDate(newsalert.PubDate);
                    strMainContent = newsalert.MainContent;
                    strBraftonID = newsalert.BraftonID;
                    strSmallImg = newsalert.SmallImg;
                    strLargeImg = newsalert.LargeImg;
                    strAltText = newsalert.AltText;

                    if (strArticleType == "WFA") 
                    {
                        ImportWeeklyFeatureArticle(strType, strTopic, strTitle, strDescription, strPubDate, strMainContent, strBraftonID, strSmallImg, strLargeImg, strAltText, strCategoryID, strCategory);
                    }
                    else
                    {
                        ImportNewsAlert(strTitle, strDescription, strPubDate, strMainContent, strBraftonID, strSmallImg, strLargeImg, strAltText, strCategoryID, strCategory);
                    }
                }
            }
        }

        private void ImportNewsAlert(string strTitle, string strDescription, string strPubDate, string strMainContent, string strBraftonID, string strSmallImg, string strLargeImg, string strAltText, string strCategoryID, string strCategory)
        {
            using (new SecurityDisabler())
            {
                Database dbMaster = Database.GetDatabase("master");
                Database dbWeb = Database.GetDatabase("web");
                TemplateItem ti = dbMaster.GetTemplate("{1E60D2DE-69A1-483E-8E1B-EE53E9681011}");

                if (ti != null)
                {
                    Item iParent = dbMaster.Items["{14CEC7CF-DE84-4915-A21E-5CD3E58F4DE6}"];

                    if (dbMaster.Items[String.Concat("/sitecore/content/GlobalElements/KnowledgeCenter/NewsAlertItems/", strBraftonID)] != null)
                    {
                        Item iMyItem = dbMaster.Items[String.Concat("/sitecore/content/GlobalElements/KnowledgeCenter/NewsAlertItems/", strBraftonID)];

                        using (new EditContext(iMyItem))
                        {
                            iMyItem.Fields["__Display Name"].Value = strTitle;
                            iMyItem.Fields["Title"].Value = strTitle;
                            iMyItem.Fields["Description"].Value = strDescription;
                            iMyItem.Fields["Date"].Value = strPubDate;
                            iMyItem.Fields["Content"].Value = strMainContent;
                            iMyItem.Fields["BraftonID"].Value = strBraftonID;
                            iMyItem.Fields["BraftonThumbnail"].Value = strSmallImg;
                            iMyItem.Fields["BraftonFullSize"].Value = strLargeImg;
                            iMyItem.Fields["BraftonAltText"].Value = strAltText;
                            iMyItem.Fields["CategoryID"].Value = strCategoryID;
                            iMyItem.Fields["Category"].Value = strCategory;
                        }
                    }
                    else
                    {
                        Item iMyItem = iParent.Add(strBraftonID, ti);

                        using (new EditContext(iMyItem))
                        {
                            iMyItem.Fields["__Display Name"].Value = strTitle;
                            iMyItem.Fields["Title"].Value = strTitle;
                            iMyItem.Fields["Description"].Value = strDescription;
                            iMyItem.Fields["Date"].Value = strPubDate;
                            iMyItem.Fields["Content"].Value = strMainContent;
                            iMyItem.Fields["BraftonID"].Value = strBraftonID;
                            iMyItem.Fields["BraftonThumbnail"].Value = strSmallImg;
                            iMyItem.Fields["BraftonFullSize"].Value = strLargeImg;
                            iMyItem.Fields["BraftonAltText"].Value = strAltText;
                            iMyItem.Fields["CategoryID"].Value = strCategoryID;
                            iMyItem.Fields["Category"].Value = strCategory;
                        }
                    }

                    PublishOptions po = new PublishOptions(dbMaster, dbWeb, PublishMode.SingleItem, Sitecore.Context.Language, DateTime.Now);
                    po.RootItem = iParent;
                    po.Deep = true; // this is publish subitems

                    (new Publisher(po)).Publish();
                }
            }
        }

        private void ImportWeeklyFeatureArticle(string strType, string strTopic, string strTitle, string strDescription, string strPubDate, string strMainContent, string strBraftonID, string strSmallImg, string strLargeImg, string strAltText, string strCategoryID, string strCategory)
        {
            using (new SecurityDisabler())
            {
                Database dbMaster = Database.GetDatabase("master");
                Database dbWeb = Database.GetDatabase("web");
                TemplateItem ti = dbMaster.GetTemplate("{3F37FA05-610A-4F71-AA94-4F2E8382AD3C}");

                if (ti != null)
                {
                    Item iParent = dbMaster.Items["{57724B9E-D85C-46A0-99C1-FAA6083A2608}"];

                    if (dbMaster.Items[String.Concat("/sitecore/content/GlobalElements/KnowledgeCenter/ArticleItems/", strBraftonID)] != null)
                        {
                            Item iMyItem = dbMaster.Items[String.Concat("/sitecore/content/GlobalElements/KnowledgeCenter/ArticleItems/", strBraftonID)];

                            using (new EditContext(iMyItem))
                            {
                                iMyItem.Fields["__Display Name"].Value = strTitle;
                                iMyItem.Fields["Type"].Value = strType;
                                iMyItem.Fields["Topic"].Value = strTopic;
                                iMyItem.Fields["Title"].Value = strTitle;
                                iMyItem.Fields["Description"].Value = strDescription;
                                iMyItem.Fields["Date"].Value = strPubDate;
                                iMyItem.Fields["Content"].Value = strMainContent;
                                iMyItem.Fields["BraftonID"].Value = strBraftonID;
                                iMyItem.Fields["BraftonThumbnail"].Value = strSmallImg;
                                iMyItem.Fields["BraftonFullSize"].Value = strLargeImg;
                                iMyItem.Fields["BraftonAltText"].Value = strAltText;
                                iMyItem.Fields["CategoryID"].Value = strCategoryID;
                                iMyItem.Fields["Category"].Value = strCategory;
                            }
                        }
                    else
                        {
                            Item iMyItem = iParent.Add(strBraftonID, ti);

                            using (new EditContext(iMyItem))
                            {
                                iMyItem.Fields["__Display Name"].Value = strTitle;
                                iMyItem.Fields["Type"].Value = strType;
                                iMyItem.Fields["Topic"].Value = strTopic;
                                iMyItem.Fields["Title"].Value = strTitle;
                                iMyItem.Fields["Description"].Value = strDescription;
                                iMyItem.Fields["Date"].Value = strPubDate;
                                iMyItem.Fields["Content"].Value = strMainContent;
                                iMyItem.Fields["BraftonID"].Value = strBraftonID;
                                iMyItem.Fields["BraftonThumbnail"].Value = strSmallImg;
                                iMyItem.Fields["BraftonFullSize"].Value = strLargeImg;
                                iMyItem.Fields["BraftonAltText"].Value = strAltText;
                                iMyItem.Fields["CategoryID"].Value = strCategoryID;
                                iMyItem.Fields["Category"].Value = strCategory;
                            }
                        }

                    PublishOptions po = new PublishOptions(dbMaster, dbWeb, PublishMode.SingleItem, Sitecore.Context.Language, DateTime.Now);
                    po.RootItem = iParent;
                    po.Deep = true; // this is publish subitems

                    (new Publisher(po)).Publish();
                }
            }
        }
    }
}
