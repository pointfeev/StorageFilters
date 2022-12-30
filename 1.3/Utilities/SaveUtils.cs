using System;
using System.IO;
using System.Reflection;
using Verse;

namespace StorageFilters.Utilities
{
    public static class SaveUtils
    {
        private const string OldFileName = "StorageFilters.pcc";
        private const string FileName = "StorageFilters.xml";

        public static string FolderPath
        {
            get
            {
                string result;
                try
                {
                    result = typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.Static | BindingFlags.NonPublic)
                                                ?.Invoke(null, new object[] { "StorageFilters" }) as string;
                }
                catch (Exception)
                {
                    Log.Warning("ASF_ModPrefix".Translate() + "ASF_SaveDirectoryError".Translate());
                    throw;
                }
                return result;
            }
        }

        public static string FilePath
        {
            get
            {
                string oldFilePath = Path.Combine(FolderPath, OldFileName);
                string filePath = Path.Combine(FolderPath, FileName);
                if (File.Exists(oldFilePath))
                {
                    if (File.Exists(filePath))
                        File.Delete(oldFilePath);
                    else
                        File.Move(oldFilePath, filePath);
                }
                return filePath;
            }
        }

        public static void Save()
        {
            try
            {
                Scribe.saver.InitSaving(FilePath, "StorageFilters");
                StorageFiltersData.ExposeSavedFilter();
            }
            catch (Exception)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_SaveError".Translate());
                throw;
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }

        public static void Load()
        {
            if (!File.Exists(FilePath))
                return;
            try
            {
                Scribe.loader.InitLoading(FilePath);
                StorageFiltersData.ExposeSavedFilter();
            }
            catch (Exception)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_LoadError".Translate());
                throw;
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }
    }
}