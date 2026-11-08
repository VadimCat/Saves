namespace Services.Saves
{
    public sealed class SaveSettings
    {
        public string FileName = "saveData.json";
        // Optional absolute path for isolated profiles/tests. Null uses persistentDataPath.
        public string DirectoryPath;
    }
}
