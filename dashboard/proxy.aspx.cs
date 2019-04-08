using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;

namespace POCStoreConnect
{
    public partial class proxy : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            WebClient wc = new WebClient();
            Response.Write(wc.DownloadString(Request["url"]));
        }
    }
}