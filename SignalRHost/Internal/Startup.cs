using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using System.Web.Services;

namespace SignalRHost.Internal
{
    public static class Startup
    {
        public static void Configuration(IAppBuilder appBuilder)

        {

             HttpConfiguration config = new HttpConfiguration();



            PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem(@".\wwwroot");

            FileServerOptions fileOptions = new FileServerOptions();



            fileOptions.EnableDefaultFiles = true;

            fileOptions.RequestPath = PathString.Empty;

            fileOptions.FileSystem = physicalFileSystem;

            fileOptions.DefaultFilesOptions.DefaultFileNames = new[] { "index.html" };

            fileOptions.StaticFileOptions.FileSystem = fileOptions.FileSystem = physicalFileSystem;

            fileOptions.StaticFileOptions.ServeUnknownFileTypes = true;

            fileOptions.EnableDirectoryBrowsing = true;



            FormatterConfig.ConfigureFormatters(config.Formatters);

            config.MapHttpAttributeRoutes();



            appBuilder.UseWebApi(config);

            appBuilder.UseFileServer(fileOptions);

        }
    }
}
