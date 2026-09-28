using System.Text;

namespace PdfmeCore;

/// <summary>レイアウトJSONと生成値クラスを一組として保存する。</summary>
public static class ReportLayoutSaveCoordinator
{
    public static void Save(LayoutDocument layout, string layoutPath)
    {
        var fullLayoutPath = Path.GetFullPath(layoutPath);
        var valuesClassPath = ReportValuesClassGenerator.GetOutputPath(fullLayoutPath);
        var layoutJson = LayoutStore.SerializeForSave(layout, fullLayoutPath);
        var valuesClass = ReportValuesClassGenerator.Generate(layout);
        Directory.CreateDirectory(Path.GetDirectoryName(fullLayoutPath)!);

        var suffix = $".{Guid.NewGuid():N}.tmp";
        var stagedLayout = fullLayoutPath + suffix;
        var stagedClass = valuesClassPath + suffix;
        var classBackup = valuesClassPath + suffix + ".bak";
        var classExisted = File.Exists(valuesClassPath);
        var classCommitted = false;
        var keepBackup = false;
        try
        {
            File.WriteAllText(stagedLayout, layoutJson, new UTF8Encoding(false));
            File.WriteAllText(stagedClass, valuesClass, new UTF8Encoding(false));
            if (classExisted) File.Copy(valuesClassPath, classBackup);

            File.Move(stagedClass, valuesClassPath, overwrite: true);
            classCommitted = true;
            File.Move(stagedLayout, fullLayoutPath, overwrite: true);
        }
        catch (Exception saveError)
        {
            if (classCommitted)
            {
                try
                {
                    if (classExisted) File.Move(classBackup, valuesClassPath, overwrite: true);
                    else File.Delete(valuesClassPath);
                }
                catch (Exception rollbackError)
                {
                    keepBackup = true;
                    throw new AggregateException("保存と復元に失敗しました。バックアップを確認してください: " + classBackup,
                        saveError, rollbackError);
                }
            }
            throw;
        }
        finally
        {
            if (File.Exists(stagedLayout)) File.Delete(stagedLayout);
            if (File.Exists(stagedClass)) File.Delete(stagedClass);
            if (!keepBackup && File.Exists(classBackup)) File.Delete(classBackup);
        }
    }
}
