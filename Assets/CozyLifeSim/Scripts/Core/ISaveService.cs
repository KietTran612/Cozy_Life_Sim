namespace CozyLifeSim.Core
{
    public interface ISaveService
    {
        SaveData ActiveSave { get; }
        void Save();
        void Load();
        void NormalizeSaveData();

#if UNITY_EDITOR
        bool ForceSaveFailure { get; set; }
#endif
    }
}
