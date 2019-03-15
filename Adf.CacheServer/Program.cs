using System;
using System.Collections.Generic;
using System.Text;

namespace Adf.CacheServer
{
    public class Program : Adf.Service.IService, Adf.Service.IHttpService
    {
        internal static CacheManager[] CacheManagers;

        public static int HASH_POOL_SIZE = Adf.ConfigHelper.GetSettingAsInt("HashPoolSize",10);

        Adf.Cs.ServerListen csListen;
        System.Threading.Thread cleanThread;

        string servername;
        Version version;


        static void Main(string[] args)
        {
            //Test();
            Adf.Service.ServiceHelper.Entry(args);
        }

        public void Start(Adf.Service.ServiceContext serviceContext)
        {
            this.servername = serviceContext.ServiceName;
            this.version = this.GetType().Assembly.GetName().Version;
            //
            var cacheManagers = new CacheManager[HASH_POOL_SIZE];
            for (int i = 0; i < HASH_POOL_SIZE; i++)
            {
                cacheManagers[i] = new CacheManager();
            }
            Program.CacheManagers = cacheManagers;
            //
            var port = Adf.ConfigHelper.GetSettingAsInt("Port");
            if (port < 1)
            {
                serviceContext.LogManager.Error.WriteTimeLine("appSetting Port invalid");
                throw new System.Configuration.ConfigurationErrorsException("appSetting Port invalid");
            }
            //
            this.csListen = new Adf.Cs.ServerListen(serviceContext.LogManager);
            //
            this.cleanThread = new System.Threading.Thread(this.CleanHandler);
            this.cleanThread.IsBackground = true;
            this.cleanThread.Start(serviceContext.LogManager);
        }

        public void Stop(Adf.Service.ServiceContext serviceContext)
        {
            try { this.cleanThread.Abort(); }
            catch { }

            this.csListen.Dispose();
        }
        
        private void CleanHandler(object userState)
        {
            var logMessage = ((LogManager)userState).Message;

            while (true)
            {
                //60 seconds
                System.Threading.Thread.Sleep(60000);
                var cleanCount = 0;

                for (int i = 0; i < HASH_POOL_SIZE; i++)
                {
                    cleanCount += Program.CacheManagers[i].CleanExpired();
                }

                logMessage.WriteTimeLine("Clean: " + cleanCount);

                //Program.SessionManager.Add(new Random().Next());
                //System.Threading.Thread.Sleep(6000);
            }
        }

        public System.Net.HttpStatusCode HttpProcess(HttpServerContext httpContext)
        {
            var hostname = System.Net.Dns.GetHostName();


            if (httpContext.Path.Equals("/favicon.ico"))
            {
                return System.Net.HttpStatusCode.NotFound;
            }
            else if (httpContext.Path.Equals("/string-view"))
            {
                this.StringView(httpContext);
                return System.Net.HttpStatusCode.OK;
            }

            var size = 100;
            var k = (httpContext.QueryString["k"] ?? "").Trim();
            var v = (httpContext.QueryString["v"] ?? "").Trim();
            var expires = Adf.ConvertHelper.ToInt32(httpContext.QueryString["expires"],60);
            var act = httpContext.QueryString["act"];

            var msg = "";
            switch (act)
            {
                case "search":
                    msg = "Search " + k + " Result:";
                    break;
                case "add":
                    var add_result = new CacheServer().Add(k, Encoding.UTF8.GetBytes(v), expires);
                    if (add_result)
                        msg = "Add " + k + " Result: <font color=\"green\">Success</font>";
                    else
                        msg = "Add " + k + " Result: <font color=\"red\">Failure or Exists</font>";
                    break;
                case "set":
                    var set_result = new CacheServer().Set(k, Encoding.UTF8.GetBytes(v), expires, expires);
                    if (set_result == 0)
                        msg = "Set " + k + " Result: <font color=\"red\">Failure</font>";
                    else if (set_result == 1)
                        msg = "Set " + k + " Result: <font color=\"green\">Replace success</font>";
                    else if (set_result == 2)
                        msg = "Set " + k + " Result: <font color=\"green\">New Cache</font>";
                    else if (set_result == 2)
                        msg = "Set " + k + " Result: ";
                    break;
                case "del":
                    var del_result = new CacheServer().Delete(k);
                    if (del_result)
                        msg = "Del " + k + " Result: <font color=\"green\">Success</font>";
                    else
                        msg = "Del " + k + " Result: <font color=\"red\">Failure or No Exists</font>"; ;
                    break;
                default:
                    msg = "Last " + size + " Cache List:";
                    break;
            }


            //count
            var count = 0;
            for (int i = 0; i < HASH_POOL_SIZE; i++)
            {
                count += Program.CacheManagers[i].Count;
            }
            //
            var build = new StringBuilder();
            build.AppendLine("<!DOCTYPE html>");
            build.AppendLine("<html>");
            build.AppendLine("<head>");

            build.AppendLine("<style type=\"text/css\">");
            build.AppendLine(".tb1{ background-color:#D5D5D5;}");
            build.AppendLine(".tb1 td{ background-color:#FFF;}");
            build.AppendLine(".tb1 tr.None td{ background-color:#FFF;}");
            build.AppendLine(".tb1 tr.Success td{ background-color:#FFF;}");
            build.AppendLine(".tb1 tr.Failed td{ background-color:#FAEBD7;}");
            build.AppendLine(".tb1 tr.Running td{ background-color:#F5FFFA;}");
            build.AppendLine("img,form{ border:0px;}");
            build.AppendLine("img.button{ cursor:pointer; }");
            build.AppendLine("a { padding-left:5px; }");
            build.AppendLine("</style>");

            build.AppendLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
            build.AppendLine("<title>" + this.servername + " Via " + hostname + "</title>");
            build.AppendLine("</head>");
            build.AppendLine("<body>");

            build.AppendLine("<div><form action=\"/\">");
            build.AppendLine("<input size=\"15\" type=\"text\" name=\"k\" placeholder=\"key\" value=\"" + k + "\"  />");
            build.AppendLine("<input size=\"15\" type=\"text\" name=\"v\" placeholder=\"value\" value=\"" + v + "\"  />");
            build.AppendLine("<input size=\"15\" type=\"text\" name=\"expires\" placeholder=\"expires seconds\" value=\"" + expires + "\"  />");
            build.AppendLine("<input type=\"hidden\" name=\"act\" value=\"search\"  />");
            build.AppendLine("<input type=\"button\" value=\"Get\" onclick=\"this.form.act.value = 'get'; this.form.submit();\" />");
            build.AppendLine("<input type=\"button\" value=\"Add\" onclick=\"this.form.act.value = 'add'; this.form.submit();\" />");
            build.AppendLine("<input type=\"button\" value=\"Set\" onclick=\"this.form.act.value = 'set'; this.form.submit();\" />");
            build.AppendLine("<input type=\"button\" value=\"Home\" onclick=\"location.href='/';\" />");
            build.AppendLine("<span style=\"float:right;\">");
            build.AppendLine("Powered by <a href=\"http://www.aooshi.org/adf\" target=\"_blank\">Adf.CacheServer</a>");
            build.Append('v');
            build.Append(version.Major);
            build.Append(".");
            build.Append(version.Minor);
            build.Append(".");
            build.Append(version.Build);
            build.AppendLine(" Via " + hostname);
            build.AppendLine(" , Pool Size: <font color=\"green\">" + HASH_POOL_SIZE + "</font>");
            build.AppendLine(" , Caches Totals: <font color=\"green\">" + count + "</font>");
            build.AppendLine("</span></form></div>");

            build.AppendLine("<div>" + msg + "</div>");
            build.AppendLine("<table class=\"tb1\" width=\"100%\" border=\"0\" cellspacing=\"1\" cellpadding=\"3\">");
            build.AppendLine("<thead>");
            build.AppendLine("<tr>");
            build.AppendLine("<th align=\"left\">Key</th>");
            build.AppendLine("<th width=\"160\">Bytes</th>");
            build.AppendLine("<th width=\"120\">Expires</th>");
            build.AppendLine("<th width=\"120\">Options</th>");
            build.AppendLine("</tr>");
            build.AppendLine("</thead>");

            List<CacheProperty> propertys = null;
            if (k == "")
            {
                propertys = this.GetPropertys(size);
            }
            else
            {
                var hashCode = HashHelper.GetHashCode(k);
                var property = Program.CacheManagers[hashCode % HASH_POOL_SIZE].GetProperty(k);

                propertys = new List<CacheProperty>();
                if (property.Expires != 0)
                {
                    propertys.Add(property);
                }
            }
            var nowTick = Environment.TickCount;
            var expireString = "";
            foreach (var p in propertys)
            {
                if (nowTick - p.Expires > -1)
                {
                    expireString = "Expired";
                }
                else
                {
                    try
                    {
                        expireString = ((p.Expires - nowTick) / 1000).ToString();
                    }
                    catch
                    {
                        expireString = "Expired";
                    }
                }

                build.AppendLine("<tbody>");
                build.AppendLine("<tr>");
                //build.AppendLine("<td align=\"left\">" + item.Id + "|"+ item.Expires +"|"+ seconds +"</td>");
                build.AppendLine("<td align=\"left\"><a href=\"string-view?k="+p.Key+"\" target=\"string-view\">" + p.Key + "</a></td>");
                build.AppendLine("<td align=\"center\">" + Adf.ByteHelper.FormatBytes( p.Size ) + "</td>");
                build.AppendLine("<td align=\"center\">" + expireString + "</td>");
                build.AppendLine("<td align=\"center\"><a href=\"?k=" + p.Key + "&act=del\" target=\"_self\">Remove</a></td>");
                build.AppendLine("</tr>");
                build.AppendLine("</tbody>");
            }

            build.AppendLine("</table>");

            build.AppendLine("</body>");
            build.AppendLine("</html>");

            //
            httpContext.Content = build.ToString();
            httpContext.ResponseHeader["Via"] = hostname;
            httpContext.ResponseHeader["Content-Type"] = "text/html";
            return System.Net.HttpStatusCode.OK;
        }

        private void StringView(HttpServerContext httpContext)
        {
            var hostname = System.Net.Dns.GetHostName();

            var k = (httpContext.QueryString["k"] ?? "").Trim();

            var build = new StringBuilder();
            build.AppendLine("<!DOCTYPE html>");
            build.AppendLine("<html>");
            build.AppendLine("<head>");
            build.AppendLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
            build.AppendLine("<title>View string data from " + this.servername + " Via " + hostname + "</title>");
            build.AppendLine("</head>");
            build.AppendLine("<body>");

            if (k == "")
            {
                build.AppendLine("<h3>no input key</h3>");
            }
            else
            {
                var v = new CacheServer().Get(k);
                if (v == null)
                    build.AppendLine("<h3>no find key : " + k + "</h3>");
                else
                    build.AppendLine("<pre>" + Encoding.UTF8.GetString(v) + "</pre>");
            }

            build.AppendLine("</body>");
            build.AppendLine("</html>");

            //
            httpContext.Content = build.ToString();
            httpContext.ResponseHeader["Via"] = hostname;
            httpContext.ResponseHeader["Content-Type"] = "text/html";
        }

        private List<CacheProperty> GetPropertys(int size)
        {
            //list
            var list = new List<CacheProperty>(size);

            //property list
            var propertys = new CacheProperty[HASH_POOL_SIZE][];
            for (int i = 0; i < HASH_POOL_SIZE; i++)
            {
                propertys[i] = Program.CacheManagers[i].GetLastProperty(size);
            }

            //
            for (int i = 0; i < size && list.Count < size; i++)
            {
                for (int j = 0; j < HASH_POOL_SIZE && list.Count < size; j++)
                {
                    if (propertys[j].Length > i)
                    {
                        var item = propertys[j][i];
                        if (item.Expires != 0)
                        {
                            list.Add(item);
                        }
                    }
                }
            }

            return list;
        }
    }
}