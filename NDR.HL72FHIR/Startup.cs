using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace NDR.HL72FHIR
{
    class Startup
    {
        public IHostingEnvironment HostingEnvironment { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            this.HostingEnvironment = env;
            this.Configuration = configuration;
        }
    }
}
