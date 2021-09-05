﻿using System;
using System.IO;

namespace hadesFirm
{
  internal class Logger
  {
    public static bool nologging = false;
    public static Form1 form;

    private static string GetTimeDate()
    {
      string empty = string.Empty;
      return DateTime.Now.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("HH:mm:ss");
    }

    private static void CleanLog()
    {
      if (Utility.run_by_cmd)
        return;
      if (Logger.form.log_textbox.InvokeRequired)
      {
        Logger.form.log_textbox.Invoke((Delegate)((Action)(() =>
        {
          if (Logger.form.log_textbox.Lines.Length <= 30)
            return;
          Logger.form.log_textbox.Text.Remove(0, Logger.form.log_textbox.GetFirstCharIndexFromLine(1));
        })));
      }
      else
      {
        if (Logger.form.log_textbox.Lines.Length <= 30)
          return;
        Logger.form.log_textbox.Text.Remove(0, Logger.form.log_textbox.GetFirstCharIndexFromLine(1));
      }
    }

    public static void WriteLog(string str, bool raw = false)
    {
      if (Logger.nologging)
        return;
      Logger.CleanLog();
      if (!raw)
        str += "\n";
      if (Utility.run_by_cmd)
        Console.Write(str);
      else if (Logger.form.log_textbox.InvokeRequired)
      {
        Logger.form.log_textbox.Invoke((Delegate)((Action)(() =>
        {
          Logger.form.log_textbox.AppendText(str);
          Logger.form.log_textbox.ScrollToCaret();
        })));
      }
      else
      {
        Logger.form.log_textbox.AppendText(str);
        Logger.form.log_textbox.ScrollToCaret();
      }
    }

    public static void SaveLog()
    {
      string AppLocation = System.AppDomain.CurrentDomain.BaseDirectory;
      string LogFile = AppLocation + "hadesFirm.log";
      string OldLogFile = AppLocation + "hadesFirm.log.old";

      if (string.IsNullOrEmpty(Logger.form.log_textbox.Text))
        return;
      if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 2097152L)
      {
        File.Delete(OldLogFile);
        File.Move(LogFile, OldLogFile);
      }
      using (TextWriter textWriter = (TextWriter) new StreamWriter((Stream) new FileStream(LogFile, FileMode.Append)))
      {
        textWriter.WriteLine();
        textWriter.WriteLine(Logger.GetTimeDate());
        foreach (string line in Logger.form.log_textbox.Lines)
          textWriter.WriteLine(line);
      }
    }
  }
}
