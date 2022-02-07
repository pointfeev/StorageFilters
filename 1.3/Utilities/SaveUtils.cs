using System;
using System.IO;
using System.Reflection;

using Verse;

namespace StorageFilters
{
    public static class SaveUtils
    {
        public static string FolderPath
        {
            get
            {
                string result;
                try
                {
                    result = typeof(GenFilePaths).GetMethod("FolderUnderSaveData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
                    {
                        "StorageFilters"
                    }) as string;
                }
                catch (Exception ex)
                {
                    Log.Warning("ASF_ModPrefix".Translate() + "ASF_SaveDirectoryError".Translate());
                    throw ex;
                }
                return result;
            }
        }

        private static readonly string oldFileName = "StorageFilters.pcc";
        private static readonly string fileName = "StorageFilters.xml";

        public static string FilePath
        {
            get
            {
                string oldFilePath = Path.Combine(FolderPath, oldFileName);
                string filePath = Path.Combine(FolderPath, fileName);
                if (File.Exists(oldFilePath))
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(oldFilePath);
                    }
                    else
                    {
                        File.Move(oldFilePath, filePath);
                    }
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
            catch (Exception ex)
            {
                Log.Warning("ASF_ModPrefix".Translate() + "ASF_SaveError".Translate());
                throw ex;
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
                Scribe.mode = LoadSaveMode.Inactive;
            }
        }

        public static void Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    Scribe.loader.InitLoading(FilePath);
                    StorageFiltersData.ExposeSavedFilter();
                }
                catch (Exception ex)
                {
                    Log.Warning("ASF_ModPrefix".Translate() + "ASF_LoadError".Translate());
                    throw ex;
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                    Scribe.mode = LoadSaveMode.Inactive;
                }
            }
        }
    }
}