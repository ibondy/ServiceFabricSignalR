namespace SignalRHost
{
    #region Using

    using System;
    using System.Fabric;

    #endregion

    public static class SignalRHostConfiguration
    {
        static SignalRHostConfiguration()
        {
            var codeContext = FabricRuntime.GetActivationContext();
            if (codeContext == null) // sanity check
                throw new ApplicationException("CodePackageActivationContext is null");

            var configurationPackage = codeContext.GetConfigurationPackageObject("Config");

            if (configurationPackage.Settings?.Sections == null || !configurationPackage.Settings.Sections.Contains("SignalRScaleout"))
                return;

            var param = configurationPackage.Settings.Sections["SignalRScaleout"].Parameters;

            if (param.Contains("UseScaleout"))
                UseScaleout = bool.Parse(param["UseScaleout"].Value);
            if (param.Contains("SFScaleoutConnectionString"))
                SFScaleoutConnectionString = param["SFScaleoutConnectionString"].Value;
            if (param.Contains("UseActors"))
                UseActors = bool.Parse(param["UseActors"].Value);
        }

        #region

        public static bool UseScaleout { get; }
        public static string SFScaleoutConnectionString { get; }

        public static bool UseActors { get; }

        #endregion
    }
}