using GA_Composer.Utils;
using Midi;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GA_Composer.Repositories
{
    public enum ChordTableEnum
    {
        CMaj7 = 0,
        Bm7 = 1,
        Em7 = 2,
        Am7 = 3,
        Gm7 = 4,
        C7 = 5,
        F7 = 6,
        Fm7 = 7,
        Dm7 = 8,
        G7 = 9
    }

    public static class ChordTableEnumExtentionMethods
    {
        public static int Index(this ChordTableEnum chord)
        {
            return (int)chord;
        }
    }

    [Serializable]
    public class Sequence
    {
        #region GridView 바인딩용

        public DataTable TheNumberOfNotesTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 1; i <= theNumberOfNotes.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < theNumberOfNotes.Length; i++)
                {
                    dr[i] = theNumberOfNotes[i].ToString();
                    dr2[i] = theNumberOfNotesDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);

                return dt;
            }
        }

        public DataTable BetweenHighAndLowTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 0; i < betweenHighAndLow.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < betweenHighAndLow.Length; i++)
                {
                    dr[i] = betweenHighAndLow[i].ToString();
                    dr2[i] = betweenHighAndLowDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);
                return dt;
            }
        }

        public DataTable TheNumberOfChangeTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 0; i < theNumberOfChange.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < theNumberOfChange.Length; i++)
                {
                    dr[i] = theNumberOfChange[i].ToString();
                    dr2[i] = theNumberOfChangeDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);
                return dt;
            }
        }

        public DataTable BetweenNotesPitchTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 0; i < betweenNotesPitch.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < betweenNotesPitch.Length; i++)
                {
                    dr[i] = betweenNotesPitch[i].ToString();
                    dr2[i] = betweenNotesPitchDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);
                return dt;
            }
        }

        public DataTable TheNumberOfNoteLengthTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 1; i <= theNumberOfNoteLength.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < theNumberOfNoteLength.Length; i++)
                {
                    dr[i] = theNumberOfNoteLength[i].ToString();
                    dr2[i] = theNumberOfNoteLengthDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);
                return dt;
            }
        }


        public DataTable PatternsTable
        {
            get
            {
                DataTable dt = new DataTable();

                for (int i = 1; i <= patterns.Length; i++)
                {
                    dt.Columns.Add(i.ToString());
                }

                DataRow dr = dt.NewRow();
                DataRow dr2 = dt.NewRow();

                for (int i = 0; i < patterns.Length; i++)
                {
                    dr[i] = patterns[i].ToString();
                    dr2[i] = patternsDevided[i].ToString();
                }

                dt.Rows.Add(dr);
                dt.Rows.Add(dr2);
                return dt;
            }
        }



        #endregion

        /// <summary>
        /// 최고음과 최저음의 피치 높이 차이
        /// </summary>
        public double[] betweenHighAndLowDevided;
        public int[] betweenHighAndLow;
        public void SetBetweenHighAndLow()
        {
            // double sum = betweenHighAndLow.Where(n => n > 0).Sum();
            double sum = betweenHighAndLow.Where(n => n > 0).Max();

            for (int i = 0; i < betweenHighAndLow.Length; i++)
            {
                if (betweenHighAndLow[i] > 0)
                    betweenHighAndLowDevided[i] = (double)betweenHighAndLow[i] / sum;
                else
                    betweenHighAndLowDevided[i] = 0;
            }
        }

        /// <summary>
        /// 오르막 내리막이 몇번 바뀌는지
        /// </summary>
        public double[] theNumberOfChangeDevided;
        public int[] theNumberOfChange;
        public void SetTheNumberOfChange()
        {
            // double sum = theNumberOfChange.Where(n => n > 0).Sum();
            double sum = theNumberOfChange.Where(n => n > 0).Max();

            for (int i = 0; i < theNumberOfChange.Length; i++)
            {
                if (theNumberOfChange[i] > 0)
                    theNumberOfChangeDevided[i] = (double)theNumberOfChange[i] / sum;
                else
                    theNumberOfChangeDevided[i] = 0;
            }
        }

        /// <summary>
        /// 노트의 개수
        /// </summary>
        public double[] theNumberOfNotesDevided;
        public int[] theNumberOfNotes;
        public void SetTheNumberOfNotes()
        {
            // double sum = theNumberOfNotes.Where(n => n > 0).Sum();
            double sum = theNumberOfNotes.Where(n => n > 0).Max();

            for (int i = 0; i < theNumberOfNotes.Length; i++)
            {
                if (theNumberOfNotes[i] > 0)
                    theNumberOfNotesDevided[i] = (double)theNumberOfNotes[i] / sum;
                else
                    theNumberOfNotesDevided[i] = 0;
            }
        }

        /// <summary>
        /// 앞 노트와 뒷 노트의 변화량
        /// </summary>
        public double[] betweenNotesPitchDevided;
        public int[] betweenNotesPitch;
        public void SetBetweenNotesPitch()
        {
            // double sum = betweenNotesPitch.Where(n => n > 0).Sum();
            double sum = betweenNotesPitch.Where(n => n > 0).Max();

            for (int i = 0; i < betweenNotesPitch.Length; i++)
            {
                if (betweenNotesPitch[i] > 0)
                    betweenNotesPitchDevided[i] = (double)betweenNotesPitch[i] / sum;
                else
                    betweenNotesPitchDevided[i] = 0;
            }
        }

        /// <summary>
        /// 각각의 노트 길이의 개수
        /// </summary>
        public double[] theNumberOfNoteLengthDevided;
        public int[] theNumberOfNoteLength;
        public void SetTheNumberOfNoteLength()
        {
            // double sum = theNumberOfNoteLength.Where(n => n > 0).Sum();
            double sum = theNumberOfNoteLength.Where(n => n > 0).Max();

            for (int i = 0; i < theNumberOfNoteLength.Length; i++)
            {
                if (theNumberOfNoteLength[i] > 0)
                    theNumberOfNoteLengthDevided[i] = (double)theNumberOfNoteLength[i] / sum;
                else
                    theNumberOfNoteLengthDevided[i] = 0;
            }
        }

        /// <summary>
        /// 음표(1)와 쉼표(0)을 패턴으로 둠
        /// </summary>
        public double[] patternsDevided;
        public int[] patterns;
        public void SetPatterns()
        {
            // double sum = theNumberOfNoteLength.Where(n => n > 0).Sum();
            double sum = patterns.Where(n => n > 0).Max();

            for (int i = 0; i < patterns.Length; i++)
            {
                if (patterns[i] > 0)
                    patternsDevided[i] = (double)patterns[i] / sum;
                else
                    patternsDevided[i] = 0;
            }
        }


        public void SetAll()
        {
            SetBetweenHighAndLow();
            SetTheNumberOfChange();
            SetTheNumberOfNotes();
            SetBetweenNotesPitch();
            SetTheNumberOfNoteLength();
            SetPatterns();
        }
    }

    public class TableRepository
    {
        public SavedTable SavedTable { get; set; }

        public double[][][] ConvertedChordTable { get; set; }

        public TableRepository()
        {
            InitChordTable();

            if (File.Exists("table.xml"))
            {
                SavedTable = Serializer.DeSerializeObject<SavedTable>("table.xml");
            }
            else
            {
                InitSequenceTable();
            }
        }

        public void InitSequenceTable()
        {
            SavedTable.SequenceTable = new Sequence();

            // 최대음과 최소음은 0음에서 1옥타브 까지만 보고 그 이상은 동등하게 본다
            SavedTable.SequenceTable.betweenHighAndLow = new int[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow];
            SavedTable.SequenceTable.betweenHighAndLowDevided = new double[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow];

            for (int i = 0; i < StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow; i++)
            {
                SavedTable.SequenceTable.betweenHighAndLow[i] = 1;
            }

            // 노트 개수는 Max Num of Note 까지만
            SavedTable.SequenceTable.theNumberOfNotes = new int[StaticRepo.ConfigRepository.MaxNumOfNote];
            SavedTable.SequenceTable.theNumberOfNotesDevided = new double[StaticRepo.ConfigRepository.MaxNumOfNote];

            for (int i=0; i<StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                SavedTable.SequenceTable.theNumberOfNotes[i] = 1;
            }

            // 노트의 변화는 노트 최대 개수 -1 까지만
            SavedTable.SequenceTable.theNumberOfChange = new int[StaticRepo.ConfigRepository.MaxNumOfNote - 1];
            SavedTable.SequenceTable.theNumberOfChangeDevided = new double[StaticRepo.ConfigRepository.MaxNumOfNote - 1];

            for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote-1; i++)
            {
                SavedTable.SequenceTable.theNumberOfChange[i] = 1;
            }
            
            // 노트 사이의 변화량도 보통은 1옥타브 이상은 벌어지지 않기 때문에 13까지만 한다(이후는 같은걸로)
            SavedTable.SequenceTable.betweenNotesPitch = new int[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenNotesPitch];
            SavedTable.SequenceTable.betweenNotesPitchDevided = new double[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenNotesPitch];

            for (int i = 0; i < StaticRepo.ConfigRepository.SequenceTable_MaxBetweenNotesPitch; i++)
            {
                SavedTable.SequenceTable.betweenNotesPitch[i] = 1;
            }

            // 노트의 길이는 1 ~ 최대 16
            SavedTable.SequenceTable.theNumberOfNoteLength = new int[StaticRepo.ConfigRepository.MaxNumOfNote];
            SavedTable.SequenceTable.theNumberOfNoteLengthDevided = new double[StaticRepo.ConfigRepository.MaxNumOfNote];

            for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                SavedTable.SequenceTable.theNumberOfNoteLength[i] = 1;
            }

            // 반 마디의 패턴으로 나올 수 있는 개수
            SavedTable.SequenceTable.patterns = new int[StaticRepo.ConfigRepository.SequenceTable_MaxPatterns];
            SavedTable.SequenceTable.patternsDevided = new double[StaticRepo.ConfigRepository.SequenceTable_MaxPatterns];

            for (int i = 0; i < StaticRepo.ConfigRepository.SequenceTable_MaxPatterns; i++)
            {
                SavedTable.SequenceTable.patterns[i] = 1;
            }


            SavedTable.SequenceTable.SetAll();
        }

        public void InitChordTable()
        {
            SavedTable = new SavedTable { Chordtable = new double[ChordEnumCount()][][] };
            ConvertedChordTable = new double[ChordEnumCount()][][];

            for (int i=0; i< ChordEnumCount(); i++)
            {
                SavedTable.Chordtable[i] = new double[13][];
                ConvertedChordTable[i] = new double[13][];

                for (int j=0; j<13; j++)
                {
                    SavedTable.Chordtable[i][j] =  new double[12];
                    ConvertedChordTable[i][j] = new double[12];
                }
            }

            #region Chord Table Init

            InitChordTableValue(ChordTableEnum.CMaj7, new Scale(new Note("C"), Scale.Ionian));
            InitChordTableValue(ChordTableEnum.C7, new Scale(new Note("C"), Scale.Mixolydian));
            InitChordTableValue(ChordTableEnum.Dm7, new Scale(new Note("D"), Scale.Dorian));
            InitChordTableValue(ChordTableEnum.Em7, new Scale(new Note("E"), Scale.Phrygian));
            InitChordTableValue(ChordTableEnum.F7, new Scale(new Note("F"), Scale.Mixolydian));
            InitChordTableValue(ChordTableEnum.Fm7, new Scale(new Note("F"), Scale.Dorian));
            InitChordTableValue(ChordTableEnum.Gm7, new Scale(new Note("G"), Scale.Dorian));
            InitChordTableValue(ChordTableEnum.G7, new Scale(new Note("G"), Scale.Mixolydian));
            InitChordTableValue(ChordTableEnum.Am7, new Scale(new Note("A"), Scale.Dorian));
            InitChordTableValue(ChordTableEnum.Bm7, new Scale(new Note("B"), Scale.Dorian));
            
            #endregion
        }

        /// <summary>
        /// Chord Table 에 있는 점수를 Normalize 한다
        /// </summary>
        public void ConvertChordTable()
        {
            double sum = 0;
            for (int i = 0; i < ChordEnumCount(); i++)
            {
                #region One Note

                sum = 0;

                for (int j = 0; j < 12; j++)
                {
                    if (SavedTable.Chordtable[i][0][j] < 0)
                    {
                        ConvertedChordTable[i][0][j] = 0;
                    }
                    else
                    {
                        ConvertedChordTable[i][0][j] = SavedTable.Chordtable[i][0][j];
                        if(sum < SavedTable.Chordtable[i][0][j])
                        {
                            sum = SavedTable.Chordtable[i][0][j];
                        }
                        // sum += SavedTable.Chordtable[i][0][j];
                    }
                }

                for (int j = 0; j < 12; j++)
                {
                    ConvertedChordTable[i][0][j] = (double)ConvertedChordTable[i][0][j] / (double)sum;
                }

                #endregion

                #region Two Note
                
                for (int j = 1; j < 13; j++)
                {
                    sum = 0;

                    for (int k = 0; k < 12; k++)
                    {
                        if (SavedTable.Chordtable[i][j][k] < 0)
                        {
                            ConvertedChordTable[i][j][k] = 0;
                        }
                        else
                        {
                            ConvertedChordTable[i][j][k] = SavedTable.Chordtable[i][j][k];
                            if(sum < SavedTable.Chordtable[i][j][k])
                            {
                                sum = SavedTable.Chordtable[i][j][k];
                            }
                            // sum += SavedTable.Chordtable[i][j][k];
                        }
                    }
                    
                    for (int k = 0; k < 12; k++)
                    {
                        if(sum != 0)
                        {
                            ConvertedChordTable[i][j][k] = (double)ConvertedChordTable[i][j][k] / (double)sum;
                        }
                        else
                        {
                            ConvertedChordTable[i][j][k] = 0;
                        }
                    }
                }
                
                #endregion
            }
        }


        /// <summary>
        /// 코드 테이블의 대상 타겟을 특정 스케일로 초기화 한다
        /// </summary>
        /// <param name="targetChord">코드 테이블에서 초기화 할 코드</param>
        /// <param name="targetScale">초기화 할 스케일</param>
        private void InitChordTableValue(ChordTableEnum targetChord, Scale targetScale)
        {
            #region 1차 : 코드의 스케일 구성원인지
            
            for (int i = 0; i < 12; i++)
            {
                if (targetScale.Contains((Pitch)i))
                {
                    SavedTable.Chordtable[targetChord.Index()][0][i] = 100;
                }
                else
                {
                    SavedTable.Chordtable[targetChord.Index()][0][i] = -50;
                }
            }

            #endregion

            #region 2차 : 그 다음 노트 구성원 인지

            for (int i = 0; i < 12; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (targetScale.Contains((Pitch)i))
                    {
                        SavedTable.Chordtable[targetChord.Index()][i + 1][j] += 50;
                    }
                    else
                    {
                        SavedTable.Chordtable[targetChord.Index()][i + 1][j] -= 75;
                    }

                    if (targetScale.Contains((Pitch)j))
                    {
                        SavedTable.Chordtable[targetChord.Index()][i + 1][j] += 50;
                    }
                    else
                    {
                        SavedTable.Chordtable[targetChord.Index()][i + 1][j] -= 75;
                    }
                }
            }

            #endregion
        }


        public int ChordEnumCount()
        {
            return Enum.GetNames(typeof(ChordTableEnum)).Length;
        }

        public double[][] GetChordTableFromChordName(ChordTableEnum cenum)
        {
            return SavedTable.Chordtable[(int)cenum];
        }
    }

    /// <summary>
    /// 핵심 테이블
    /// 실제로 파일로 저장되기 때문에 Serializable 해야한다
    /// </summary>
    [Serializable]
    public class SavedTable
    {
        public double[][][] Chordtable { get; set; }

        public Sequence SequenceTable { get; set; }
    }
}

