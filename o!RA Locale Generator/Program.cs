﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Google.GData.Spreadsheets;

namespace o_RA_Locale_Generator
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            SpreadsheetsService service = new SpreadsheetsService("o!RALP");
            service.setUserCredentials(GlobalVars.Username, GlobalVars.Password);
            SpreadsheetQuery query = new SpreadsheetQuery();
            SpreadsheetFeed feed = service.Query(query);
            SpreadsheetEntry ProjectSheet = null;
            foreach (SpreadsheetEntry entry in feed.Entries)
            {
                if (entry.Title.Text == @"o!RA Localization")
                {
                    ProjectSheet = entry;
                }
            }
            WorksheetEntry worksheet = (WorksheetEntry)ProjectSheet.Worksheets.Entries[0];

            CellQuery cellQuery = new CellQuery(worksheet.CellFeedLink);
            cellQuery.MinimumRow = 5;
            cellQuery.MinimumColumn = 1;
            cellQuery.MaximumColumn = 1;
            CellFeed cellFeed = service.Query(cellQuery);
            List<string> ProgramStrings = new List<string>();
            foreach (CellEntry cell in cellFeed.Entries)
            {
                ProgramStrings.Add(cell.Value);
                Console.WriteLine("{0} -> {1}", cell.Title.Text, cell.Value);
            }
            for (uint i = 2; i <= worksheet.Cols; i++ )
            {
                cellQuery.MinimumRow = 2;
                cellQuery.MinimumColumn = i;
                cellQuery.MaximumColumn = i;
                cellQuery.ReturnEmpty = ReturnEmptyCells.yes;
                cellFeed = service.Query(cellQuery);
                Locale loc = new Locale
                {
                    Features = new List<LocaleElement>()
                };

                string language = "";
                bool skip = false;
                for (int c = 0; c < cellFeed.Entries.Count; c++)
                {
                    if (c > ProgramStrings.Count + 2)
                        break;
                    switch (c)
                    {
                        case 0:
                            language = ((CellEntry)cellFeed.Entries[c]).Value;
                            if (language == "Locale Code")
                                skip = true;
                            break;
                        case 1: case 2:
                            break;
                        default:
                            loc.Features.Add(new LocaleElement { Key = ProgramStrings[c - 3], Value = ((CellEntry)cellFeed.Entries[c]).Value });
                            break;
                    }
                }
#if DEBUG
                string targetDirectory = Directory.GetParent(Directory.GetParent(Directory.GetParent(Application.StartupPath).FullName).FullName).FullName + @"\o!RA\bin\Debug\Locales\";
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);
#else
                string targetDirectory = Directory.GetParent(Directory.GetParent(Directory.GetParent(Application.StartupPath).FullName).FullName).FullName + @"\o!RA\bin\Release\Locales\";
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);
#endif
                if (!skip)
                    XMLHelper.Serialize(targetDirectory + language + ".xml", loc);
            }
        }
    }
}
