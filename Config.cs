using System.ComponentModel;
using spelunky.script.archipelago.Template.Configuration;

namespace spelunky.script.archipelago.Configuration;

public class Config : Configurable<Config>
{
    /*
        User Properties:
            - Please put all of your configurable properties here.
    
        By default, configuration saves as "Config.json" in mod user config folder.    
        Need more config files/classes? See Configuration.cs
    
        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */
    [Category("Archipelago")]
    [DisplayName("Slot Name")]
    [Description("The slot hame of this client.")]
    [DefaultValue("")]
    public string SlotName { get; set; } = "";

    [Category("Archipelago")]
    [DisplayName("Hostname")]
    [Description("The hostname of the Archipelago server, can include protocol and port.")]
    [DefaultValue("archipelago.gg")]
    public string Hostname { get; set; } = "archipelago.gg";

    [Category("Archipelago")]
    [DisplayName("Port")]
    [Description("The port number which the Archipelago server is hosted on. Will be ignored if the port is added to the hostname")]
    [DefaultValue(38281)]
    public int Port { get; set; } = 38281;

    [Category("Development")]
    [DisplayName("Enable ImGui Interface")]
    [Description("Enables the ImGui interface for debugging during development.")]
    [DefaultValue(false)]
    public bool EnableImGui { get; set; } = false;

	[Category("Development")]
	[DisplayName("Original Save")]
	[Description("Uses the original save file instead of creating a new one.")]
	[DefaultValue(false)]
	public bool OriginalSave { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
