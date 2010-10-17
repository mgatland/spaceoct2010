using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienGameSample;

namespace SpaceOctopus
{
    class CreditsScreen: MenuScreen
    {
        public CreditsScreen() : base("credits") 
        {
            //Hack: display credits as menu entries, which they are not
            AddCreditsLine("by Matthew Gatland");
            AddCreditsLine("Sound Effects by Eugen Sopot");
            AddCreditsLine("Soundtrack:");
            AddCreditsLine("Angustia by BrunoXe");

            MenuEntry exitMenuEntry = new MenuEntry("BACK");
            exitMenuEntry.Selected += OnCancel;
            MenuEntries.Add(exitMenuEntry);
        }

        private void AddCreditsLine(string text)
        {
            MenuEntry creditLine = new MenuEntry(text);
            creditLine.ExtraSpacing = 10;
            MenuEntries.Add(creditLine);
        }
    }
}
