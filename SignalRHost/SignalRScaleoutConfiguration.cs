using System;
using System.Fabric;

namespace SignalRHost
{
    public static class SignalRScaleoutConfiguration
    {
        public static bool UseScaleout { get; }
        public static string SFConnectionString { get; }

        static SignalRScaleoutConfiguration()
        {
            var codeContext = FabricRuntime.GetActivationContext();
            if (codeContext == null) // sanity check
                throw new ApplicationException("CodePackageActivationContext is null");

            ConfigurationPackage configurationPackage = codeContext.GetConfigurationPackageObject("Config");

            if (configurationPackage.Settings?.Sections == null || !configurationPackage.Settings.Sections.Contains("SignalRScaleout"))
                return;

            var param = configurationPackage.Settings.Sections["SignalRScaleout"].Parameters;

            if (param.Contains("UseScaleout"))
                UseScaleout = bool.Parse(param["UseScaleout"].Value);
            if (param.Contains("SFConnectionString"))
                SFConnectionString = param["SFConnectionString"].Value;
  
        }
    }
}
