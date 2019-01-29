using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScrapySharp.Network;
using ScrapySharp.Html.Forms;
using HtmlAgilityPack;
using ScrapySharp.Extensions;

namespace Freya
{
    class AutoDMS
    {
        private FRegSetting Regsetting;
        private ScrapingBrowser browser;
        private string URLBase = "http://10.202.10.94";
        private LogWriter log;
        public bool loggedin { get; private set; }

        private WebPage resultsPage;
        public PageWebForm form { get; private set; }
        public List<string> result;

        public AutoDMS(FRegSetting reg, bool enableTextLog = false)
        {
            browser = new ScrapingBrowser();
            browser.KeepAlive = true;
            Regsetting = reg;
            result = new List<string>();
            log = new LogWriter(enableTextLog);
            loggedin = false;
            log.WriteLine("AutoDMS initialized.");

            result.Add("Start AutoDMS Process...");

            Login();
        }

        private void Login()
        {
            if (Regsetting.DMS_Setting.UserID.Length == 0 || Regsetting.DMS_Setting.Password.Length == 0)
            {
                result.Add("No UserID/Pasword, Can't Login.");
                log.WriteLine("No UserID/Pasword, Can't Login.");
                loggedin = false;
                return;
            }

            try
            {
                resultsPage = browser.NavigateToPage(new Uri(URLBase + "/DMS/index.jsp"));

                form = resultsPage.FindForm("loginActionForm");
                form["personcode"] = Regsetting.DMS_Setting.UserID;
                form["password"] = Regsetting.DMS_Setting.getPassword();
                form.Method = HttpVerb.Post;
                resultsPage = form.Submit();
                log.WriteLine("Login form submitted.");
#if (DEBUG)
                string cookie = resultsPage.RawRequest.Headers.FirstOrDefault(c => (c.Key == "Set-Cookie") || (c.Key == "Cookie")).Value;
                //cookie = cookie.Substring(cookie.IndexOf("="), cookie.Length);
                result.Add("[D]DMS Login with Session: " + cookie);
#endif
                var valid = resultsPage.Html.CssSelect("h3");
                if (valid.Count() > 0)
                {
                    result.Add(resultsPage.Html.CssSelect("h3").First()?.InnerText?.Trim());
                    result.Add(resultsPage.Html.CssSelect("ul").First().InnerText.Trim());
                    loggedin = false;
                }
                else
                    loggedin = true;
            }
            catch (Exception ex)
            {
                result.Add("Connection fail : " + ex.Message + " -> " + ex.InnerException.Message);
                log.WriteLine("Login fail : " + ex.Message + " -> " + ex.InnerException.Message);
                loggedin = false;
            }
        }

        ~AutoDMS()
        {
            //Logout, 結束session
            if (loggedin)
                browser.NavigateToPage(new Uri(URLBase + "/DMS/logoffAction.do"));
            log.WriteLine("Logout.");
        }

        public bool UpdateDailyReport()
        {
            if (!loggedin)
            {
                result.Add("Not Loggedin, can't perform futher action.");
                log.WriteLine("Not Loggedin, can't perform futher action.[UpdateDailyReport)");
                return false;
            }
            result.Add("DMS start updating daily report.");
            // Get project list
            List<Dictionary<string, string>> projects = getProjectList();

            // Select Project
            Dictionary<string, string> project = projects.Find(p => p["projectcode"] + p["fabid"] + p["workitemcode"] == Regsetting.DMS_Setting.project);
            if (project == null)
            {
                result.Add((Regsetting.DMS_Setting.project == "auto") ?
                    "Select first On-Schedule prject automatically." :
                    "Can't find pre-defined project, select first On-Schedule project automatically.");
                project = projects.Find(p => p["datestatus"] == "On-Schedule");
                if (project == null)
                {
                    result.Add("Can't find any On-Schedule project, select first available project.");
                    project = projects.First();
                    if (project == null)
                    {
                        result.Add("Can't find any available project.");
                        return false;
                    }
                }
            }


            try
            {
                resultsPage = browser.NavigateToPage(new Uri(URLBase + "/DMS/dailyreport/dailyRecordMaintain.jsp"));
                form = resultsPage.FindForm("form_send");
                form["projectcode"] = project["projectcode"];
                form["fabid"] = project["fabid"];
                form["workitemcode"] = project["workitemcode"];
                form["workitemname"] = project["workitemname"];
                form["status"] = project["status"];
                form["isstudy"] = "N";
                form.Method = HttpVerb.Post;
                resultsPage = form.Submit();

                // Submit content
                string date = DateTime.Now.ToString("yyyy-MM-dd");
                //string date = new DateTime(2018, 12, 11, 0, 0, 0).ToString("yyyy-MM-dd");
                form = resultsPage.FindForm("dailyreportActionForm");
                string content = getContent();
                form["content"] = (content.Length > 0) ? content : "Working on " + project["projectname"] + " " + project["workitemname"];
                form["ftime"] = date + Regsetting.DMS_Setting.From.ToString(" HH:mm:00"); //2018-12-06 08:00:00
                form["ttime"] = date + Regsetting.DMS_Setting.To.ToString(" HH:mm:00");
                form["hours"] = Regsetting.DMS_Setting.To.TimeOfDay.Subtract(Regsetting.DMS_Setting.From.TimeOfDay).TotalHours.ToString();
                form.Method = HttpVerb.Post;
                resultsPage = form.Submit();

                result.Add(string.Format("Form {0} to {1} ({2} hours)", form["ftime"], form["ttime"], form["hours"]));
                result.Add(string.Format("Project : {0} / {1}", project["projectname"], project["workitemname"]));

                if (resultsPage.Html.CssSelect("title").ToArray().FirstOrDefault().InnerText.Equals("displayDailyWorkitemsInformation"))
                {
                    result.Add(string.Format("Content : {0}", form["content"]));
                    return true;
                }
                else
                {
                    result.Add(resultsPage.Html.CssSelect("h3").First().InnerText.Trim());
                    result.Add(resultsPage.Html.CssSelect("ul").First().InnerText.Trim());
                    return false;
                }
            }
            catch (Exception ex)
            {
                result.Add("Connection error : " + ex.Message + " -> " + ex.InnerException.Message);
                log.WriteLine("Connection error : (UpdateDailyReport)" + ex.Message + " -> " + ex.InnerException.Message);
                return false;
            }

        }

        public List<Dictionary<string, string>> getProjectList()
        {
            if (!loggedin)
            {
                result.Add("Not Loggedin, can't perform futher action.");
                return null;
            }

            try
            {
                resultsPage = browser.NavigateToPage(new Uri(URLBase + "/DMS/displayDailyWorkitemsAction.do"));
                HtmlNode[] projhtml = resultsPage.Html.CssSelect("table tr").ToArray();

                List<Dictionary<string, string>> projects = new List<Dictionary<string, string>>();
                string currentprojectname = "";

                foreach (var row in projhtml)
                {
                    HtmlNode[] columns = row.CssSelect("td").ToArray();
                    if (columns[0].HasClass("TitleTR")) //標題列
                        continue;

                    Dictionary<string, string> project = new Dictionary<string, string>();

                    //onclick="selectData('D20180824015320','','A','20180824015427','ENT18 Sustaining','WR','');"
                    //function selectData(projectcode,projectname,fabid,workitemcode,workitemname,status,productcode)
                    string[] projectinfo = row.CssSelect("input[name='selection']").Single().Attributes.FirstOrDefault(a => a.Name.ToLower() == "onclick").Value.Split(',');

                    if (columns[1].HasClass("DataTR"))
                        currentprojectname = columns[1].InnerText.Replace("&nbsp;", "").Trim(new char[] { '\r', '\n', '\t' });

                    project.Add("projectname", currentprojectname);
                    project.Add("projectcode", projectinfo[0].Between("'", "'"));
                    project.Add("fabid", projectinfo[2].Between("'", "'"));
                    project.Add("workitemcode", projectinfo[3].Between("'", "'"));
                    project.Add("workitemname", projectinfo[4].Between("'", "'"));
                    project.Add("status", projectinfo[5].Between("'", "'"));
                    project.Add("datestatus", columns[10].InnerText.Replace("&nbsp;", "").Trim(new char[] { '\r', '\n', '\t' }));

                    projects.Add(project);
                }

                return projects;
            }
            catch (Exception ex)
            {
                result.Add("Connection error : " + ex.Message + " -> " + ex.InnerException.Message);
                log.WriteLine("Connection error : (getProjects)" + ex.Message + " -> " + ex.InnerException.Message);
                return null;
            }
        }

        public string getContent()
        {
            return getContent(Regsetting.DMS_Setting.Items, Regsetting.DMS_Setting.Target, Regsetting.DMS_Setting.Action, Regsetting.DMS_Setting.Event);
        }

        public static string getContent(int Items, string target, string action, string events)
        {
            List<string> contents = new List<string>();

            if (Items < 1) Items = 1;

            string[] targets = target.Split(',');
            string[] actions = action.Split(',');
            string[] eventss = events.Split(',');

            //產生所有target/ action組合
            for (int i = 0; i < targets.Length; i++)
                if (targets[i].Trim().Length > 0)
                    for (int j = 0; j < actions.Length; j++)
                        if (actions[j].Trim().Length > 0)
                            contents.Add(targets[i].Trim() + " " + actions[j].Trim() + ".");

            for (int i = 0; i < eventss.Length; i++)
                if (eventss[i].Trim().Length > 0)
                    contents.Add(eventss[i].Trim() + ".");

            Random rand = new Random();
            contents = contents.OrderBy(a => rand.Next()).ToList(); //打亂排序
            contents.RemoveRange(0, Math.Max(0, contents.Count - Items));//留下指定數量
            return String.Join("\r\n", contents);
        }
    }





}
