/// <summary>
/// CLI command handlers.
/// </summary>
static class Commands
{
    public static int ShowVersion()
    {
        Console.WriteLine($"mill {Mill.Version}");
        return 0;
    }

    public static int ShowHelp()
    {
        Console.WriteLine("""
            usage:
              mill                    open workbench UI
              mill --api-only         run API server only (for dev)

              mill init               initialize repo for mill
              mill spec               create specification → GitHub issue
              mill spec <number>      refine existing spec against codebase
              mill personas           create or update user personas
              mill standards          infer or update codebase standards

              mill run                list available issues
              mill run --auto         autopick best issue
              mill run <number>       execute work loop on issue

              mill install            install or update mill
              mill --version          show version
            """);
        return 0;
    }

    public static async Task<int> InstallCheck()
    {
        var check = await Installer.Check();

        Console.WriteLine($"  version: {check.Current}");
        Console.WriteLine($"  mode:    {(check.Mode == InstallMode.Install ? "install" : "update")}");

        if (check.Mode == InstallMode.Install)
        {
            if (check.InstalledVersion != null)
            {
                Console.WriteLine($"  installed: {check.InstalledVersion}");
                if (check.IsDowngrade)
                {
                    Out.Blank();
                    Out.Warn($"would downgrade from {check.InstalledVersion} to {check.Current}");
                }
            }
            Out.Blank();
            Out.Ok("ready to install");
            Out.Detail($"binary: {Installer.GetBinaryPath()}");
            Out.Detail($"data: {Installer.GetDataPath()}");
        }
        else
        {
            if (check.Error != null)
            {
                Out.Error($"failed to check: {check.Error}");
                return 1;
            }

            Console.WriteLine($"  latest:  {check.Latest}");
            Out.Blank();

            if (check.UpdateAvailable)
                Console.WriteLine("  run `mill install` to update");
            else
                Out.Ok("already at latest");
        }

        return 0;
    }

    public static async Task<int> Install()
    {
        var check = await Installer.Check();

        Console.WriteLine($"  version: {check.Current}");
        Console.WriteLine($"  mode:    {(check.Mode == InstallMode.Install ? "install" : "update")}");

        if (check.Mode == InstallMode.Install)
        {
            if (check.InstalledVersion != null)
            {
                Console.WriteLine($"  installed: {check.InstalledVersion}");
                if (check.IsDowngrade && !Out.Confirm("downgrade?"))
                {
                    Out.Warn("aborted");
                    return 0;
                }
            }
            Out.Blank();

            var (success, message) = Installer.CopyToSystem();
            if (success)
            {
                Out.Ok(message);

                if (!Installer.IsInPath())
                {
                    Out.Blank();
                    Out.Warn("not in PATH");
                    Out.Detail(Installer.GetPathInstructions());
                }
                return 0;
            }
            else
            {
                Out.Error(message);
                return 1;
            }
        }
        else
        {
            if (check.Error != null)
            {
                Out.Error($"failed to check: {check.Error}");
                return 1;
            }

            if (!check.UpdateAvailable)
            {
                Out.Blank();
                Out.Ok($"already at latest ({check.Current})");
                return 0;
            }

            Console.WriteLine($"  latest:  {check.Latest}");
            Out.Blank();

            var (success, message) = await Installer.Update();
            if (success)
            {
                Out.Ok(message);
                return 0;
            }
            else
            {
                Out.Error(message);
                return 1;
            }
        }
    }
}
