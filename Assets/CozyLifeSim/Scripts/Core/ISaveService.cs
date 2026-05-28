namespace CozyLifeSim.Core
{
    public interface ISaveService
    {
        SaveData ActiveSave { get; }
        void Save();
        void Load();
    }
}
