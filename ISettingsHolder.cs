namespace Pololu.Usc
{
    /// <summary>
    /// An interface to be used by anything that can store usc parameter values.
    /// </summary>
    /// <remarks>
    /// When implementing setUscSettings and getUscSettings in your class, look
    /// at a saved configuration file to make sure you have handled every setting.
    /// </remarks>
    public interface ISettingsHolder
    {
        void SetUscSettings(UscSettings settings, bool newScript);
        UscSettings GetUscSettings();
    }
}