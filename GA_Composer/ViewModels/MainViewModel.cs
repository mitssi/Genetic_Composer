using GA_Composer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.WinControls.UI;
using Telerik.WinControls.UI.Docking;

namespace GA_Composer.ViewModels
{
    public class MainViewModel
    {
        #region Main Control

        public DocumentWindow ScoreDocument { get; set; }
        public DocumentWindow SequenceTableDocument { get; set; }
        public DocumentWindow ChordTableDocument { get; set; }


        // 스크롤을 강제적을 발생새키는 방법을 몰라서 컨트롤을 통하여 강제적으로 발생시킴
        public RadLabel ScrollingLabel { get; set; }
        public RadButtonElement Play { get; set; }
        public RadButtonElement Pause { get; set; }
        public RadButtonElement Stop { get; set; }
        public RadTextBoxElement TempoTextBox { get; set; }
        public RadGridView SequenceTable_TheNumberOfNotes { get; set; }
        public RadGridView SequenceTable_BetweenHighAndLow { get; set; }
        public RadGridView SequenceTable_TheNumberOfChanges { get; set; }
        public RadGridView SequenceTable_BetweenPitchofNotes { get; set; }
        public RadGridView SequenceTable_TheNumberOfNoteLengths { get; set; }
        public RadGridView SequenceTable_Patterns { get; set; }

        public RadTextBoxElement Fitness1 { get; set; }
        public RadTextBoxElement Fitness2 { get; set; }
        public RadTextBoxElement Fitness3 { get; set; }
        public RadTextBoxElement Fitness4 { get; set; }
        public RadTextBoxElement Fitness5 { get; set; }
        public RadTextBoxElement Fitness6 { get; set; }
        public RadTextBoxElement Fitness7 { get; set; }
        public RadTextBoxElement Fitness8 { get; set; }
        public RadTextBoxElement Fitness9 { get; set; }

        public RadLabel Moniter_generation { get; set; }
        public RadLabel Moniter_bestfitness { get; set; }
        public RadLabel Moniter_averagefitness { get; set; }
        public RadLabel Moniter_rouletteK { get; set; }
        public RadLabel Moniter_population { get; set; }
        public RadLabel Moniter_mutation { get; set; }
        public RadLabel Moniter_legatopercentage { get; set; }
        public RadLabel Moniter_elitism { get; set; }

        #endregion

        #region Data Binding Function

        /// <summary>
        /// One time data binding
        /// </summary>
        public void SqeuenceTableDataBinding()
        {
            SequenceTable_TheNumberOfNotes.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.TheNumberOfNotesTable;
            SequenceTable_BetweenHighAndLow.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.BetweenHighAndLowTable;
            SequenceTable_TheNumberOfChanges.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.TheNumberOfChangeTable;
            SequenceTable_BetweenPitchofNotes.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.BetweenNotesPitchTable;
            SequenceTable_TheNumberOfNoteLengths.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.TheNumberOfNoteLengthTable;
            SequenceTable_Patterns.DataSource = StaticRepo.TableRepository.SavedTable.SequenceTable.PatternsTable;
        }

        #endregion
    }
}
