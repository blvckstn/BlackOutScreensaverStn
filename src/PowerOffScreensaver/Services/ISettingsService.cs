namespace PowerOffScreensaver.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
