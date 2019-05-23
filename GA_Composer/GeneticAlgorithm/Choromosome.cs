using GA_Composer.Enums;
using GA_Composer.Models;
using GA_Composer.Repositories;
using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.GeneticAlgorithm
{
    /// <summary>
    /// 염색체가 되는 Note. 
    /// Pitch 는 국제 표준을 따르고, duration 은 1박자를 960으로 본다.
    /// </summary>
    public class GeneNote
    {
        public int pitch;
        public int duration;
    }


    public class Chromosome
    {
        public int Length;
        public GeneNote[] Note;
        public double Fitness;
        public int BarNumber;
        public double[] Fitness_Detail;

        public Chromosome(int barNumber)
        {
            Length = 0;
            // Length 는 음 길이를 알리는 용도로만. 일단 공간은 최대로 확보해 놓는다.
            Note = new GeneNote[StaticRepo.ConfigRepository.MaxNumOfNote];

            for(int i=0; i< StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                Note[i] = new GeneNote();
            }

            BarNumber = barNumber;
            Fitness_Detail = new double[Enum.GetNames(typeof(GA_AnalEnum)).Length];
        }

        public Chromosome(int barNumber, GeneNote[] notes)
        {
            Length = notes.Length;
        }

        public void InitRandomGene()
        {
            GenerateRandomNotes();
            CheckNotes();
            CalcFitness(StaticRepo.ScoreRepository.ChordListUsingGA[BarNumber]);
        }

        /// <summary>
        /// 랜덤한 음표를 만드는 작업
        /// </summary>
        public void GenerateRandomNotes()
        {
            int i = 0;
            while(Note.Sum(note => note.duration) < StaticRepo.ConfigRepository.TickPerOneBar)
            {
                Note[i].pitch = StaticRepo.ConfigRepository.GlobalRandom.Next(0,5) == 0 ? 0 : StaticRepo.ConfigRepository.GlobalRandom.Next((int)Pitch.A3, (int)Pitch.C6);
                Note[i].duration = StaticRepo.ConfigRepository.GlobalRandom.Next(1, 6) * 320;
                //Note[i].duration = StaticRepo.ConfigRepository.GlobalRandom.Next(2, 9)*240;
                i++;
            }

            Length = i;
        }

        /// <summary>
        /// 한 마디에 있는 노트들이 변이한다.
        /// 각 노트가 변이할 확률
        /// </summary>
        /// <param name="Percentage">각 노트가 변이할 확률</param>
        /// <param name="Valiation">현재 pitch 에서 변동 폭</param>
        public void Mutate(double Percentage, int Valiation)
        {
            for (int i=0; i < Length; i++)
            {
                // 피치 변조
                if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 100) < Percentage * 100)
                {
                    if (Note[i].pitch != 0)
                    {
                        int change = StaticRepo.ConfigRepository.GlobalRandom.Next(-Valiation, Valiation);
                        if ((int)GeneticPitch.C3.GeneticPitchToMidiPitch(false) <= Note[i].pitch + change
                            && (int)GeneticPitch.B6.GeneticPitchToMidiPitch(false) >= Note[i].pitch + change)
                        {
                            Note[i].pitch += change;
                            if (!StaticRepo.ConfigRepository.thisSclae.Contains((Pitch)Note[i].pitch))
                            {
                                Note[i].pitch += 1;
                            }
                        }
                        else
                        {
                            Note[i].pitch -= change;
                            if (!StaticRepo.ConfigRepository.thisSclae.Contains((Pitch)Note[i].pitch))
                            {
                                Note[i].pitch -= 1;
                            }
                        }
                    }
                }

                if (Note[i].pitch != 0)
                {
                    // 노트 삭제
                    if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 100) < Percentage * 50)
                    {
                        if (i != 0 && Length > 12)
                        {
                            Note[i - 1].duration += Note[i].duration;
                            for (int j = i; j < Length - 1; j++) // 앞으로 하나씩 땡긴다
                            {
                                Note[j] = Note[j + 1];
                            }
                            Note[Length - 1] = new GeneNote(); // 없앴으니 맨 마지막 노트는 그냥 땜빵용 노트로
                            Length--;
                        }
                    }
                }

                // 리페어링
                if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 100) < Percentage * 100)
                {
                    // 20%확률로 앞이 변경
                    if (StaticRepo.ConfigRepository.GlobalRandom.Next(1, 5) == 1)
                    {
                        if (BarNumber != 0)
                        {
                            if (Note[0].pitch != 0 && (int)StaticRepo.ScoreRepository.GABarList[BarNumber - 1].Notes.Last().ToPitch != 0)
                            {
                                // 앞에 끝 노트가 내 노트보다 작거나 크다면 내 노트를 낮추거나 높인다. 앞의 노트는 매우 중요하니 이 확률은 25%로..
                                int between = (int)StaticRepo.ScoreRepository.GABarList[BarNumber - 1].Notes.Last().ToPitch - Note[0].pitch;
                                Note[0].pitch += between / 2;
                            }
                        }

                        //if(Note[i].pitch != 0)
                        //{
                        //    if (!StaticRepo.ConfigRepository.thisSclae.Contains((Pitch)Note[i].pitch))
                        //    {
                        //        Note[0].pitch -= 1;
                        //    }
                        //}
                    }
                    else
                    {
                        if (BarNumber != StaticRepo.ScoreRepository.GABarList.Count() - 1)
                        {
                            if (Note[Length - 1].pitch != 0 && (int)StaticRepo.ScoreRepository.GABarList[BarNumber + 1].Notes[0].ToPitch != 0)
                            {
                                // 뒤에 끝 노트가 내 노트보다 작거나 크다면 내 노트를 낮추거나 높인다.
                                int between = (int)StaticRepo.ScoreRepository.GABarList[BarNumber + 1].Notes[0].ToPitch - Note[Length - 1].pitch;
                                Note[Length - 1].pitch += between / 2;
                            }
                        }

                        //if(Note[Length - 1].pitch != 0)
                        //{
                        //    if (!StaticRepo.ConfigRepository.thisSclae.Contains((Pitch)Note[Length - 1].pitch))
                        //    {
                        //        Note[Length - 1].pitch += 1;
                        //    }
                        //}
                    }
                }
            }
        }

        /// <summary>
        /// 음표들의 길이가 총 박자 수를 넘어가는지 확인한다
        /// </summary>
        public void CheckNotes()
        {
            int exceed = Note.Sum(note => note.duration) - 3840;
            Note[Length - 1].duration -= exceed;
        }

        public void MutateLegato(double legatopercentage)
        {
            if(Length > 6)
            {
                for (int i = 0; i < Length - 1; i++)
                {
                    if (Note[i].pitch == Note[i + 1].pitch)
                    {
                        if (StaticRepo.ConfigRepository.GlobalRandom.Next(0, 100) < legatopercentage * 100)
                        {
                            Note[i].duration += Note[i + 1].duration;

                            for (int j = i; j < Length - 2; j++)
                            {
                                Note[j + 1].pitch = Note[j + 2].pitch;
                                Note[j + 1].duration = Note[j + 2].duration;
                            }

                            Length--;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 사실상 프로그램의 핵심이 되는 함수
        /// Normalized Chord Table 을 참조하여 Chromosome(Bar)의 Fitness 를 구한다.
        /// </summary>
        /// <remarks>
        /// 반드시 이 함수를 사용하기 전에 Chord Table 을 참고하여 Normalize 를 해야한다.
        /// 즉 StaticRepo.TableRepository.ConvertChordTable() 을 실행해야 한다.
        /// 여기서 하지 않는 이뉴는 이 함수가 너무나도 많이 호출되는데 이걸 일일히 다 하려면 매우 많은 낭비가 있기 때문이다.
        /// </remarks>
        /// <param name="chords">해당 Bar 에 있는 코드. 
        /// Bar가 가지고 있는 최대 음표만큼 분할한 배열이 들어간다. </param>
        /// <returns>Fitness of this bar</returns>
        public void CalcFitness(ChordTableEnum[] chords)
        {
            double Fitness1 = 0; // 노트 하나에 대한 Fitness = One note table
            double Fitness2 = 0; // 이어지는 노트에 대한 Fitness = Two notes table
            double Fitness3 = 0; // 노트 개수에 대한 Fitness = The number of notes&rests
            double Fitness4 = 0; // 마디 하나에서 피치의 최고점과 최저점과의 차이 = The number of inflections
            double Fitness5 = 0; // 변곡점의 개수 = The number of inflections
            double Fitness6 = 0; // 연속된 노트 피치의 차이들 계산 = Pitch difference of notes
            double Fitness7 = 0; // Note 들의 길이들 = The number of lengths
            double Fitness8 = 0; // 시작 지점에 대한 점수
            double Fitness9 = 0; // 재즈 리듬 스코어

            #region Chord Table 에서 노트 하나에 대한 Fitness

            int index = 0;
            int remainlength = Note[0].duration;
            int fitness1Count = 0;

            for (int i = 0; i < StaticRepo.ConfigRepository.MaxNumOfNote; i++)
            {
                if(Note[i].pitch != 0)
                {
                    fitness1Count++;

                    if (remainlength == 0)
                    {
                        remainlength = Note[++index].duration;
                    }

                    // 현재 노트에 대한 fitness 구한다
                    Fitness1 += StaticRepo.TableRepository.ConvertedChordTable
                        [chords[i].Index()] // 어떤 코드인지
                        [0] // 해당 노트에 대한 점수를 얻는 열
                        [(Note[index].pitch) % 12]; // 해당 노트의 점수

                    remainlength -= StaticRepo.ConfigRepository.TickPerMinNote;
                }
            }

            if (fitness1Count == 0)
                Fitness1 = 0;
            else
                Fitness1 /= fitness1Count;

            #endregion

            #region Patterns

            int bitoffset = 0; // bit[6] 안의 index 로 쓸 offset -> MAX = 6
            int noteoffset = 0; // 한 음에서의 위치를 기록한 offset
            int baroffset = 0; // 하나의 마디 안에서의 위치를 기록한 offset -> MAX = 3840
            int[] bit = new int[6]; // 101101 이면 'The offset of start time' Table 에서 45의 index를 가지는 값을 참조

            foreach (var v in Note)
            {
                noteoffset = 0;
                bool flag = true; // 재즈리듬 스코어를 올리는 일은 하나의 음표에 대해서 한번만 실행

                while (noteoffset * 320 < v.duration) // v.duration 은 한 박자에 960 라서 8분 셋잇단음표는 320
                {
                    // bit[(offset + noteoffset) % 6] = v.pitch == 0 ? 0 : 1;
                    bit[bitoffset % 6] = 1;

                    noteoffset += 1;

                    if((bitoffset % 6) == 0 || (bitoffset % 6) == 2 || (bitoffset % 6) == 3 || (bitoffset % 6) == 5)
                    {
                        if(v.pitch != 0 && flag == true) // 셔플리듬에서 중요한 박자는 될 수 있으면 노트로 한다
                        {
                            Fitness9 += 0.125;
                            flag = false;
                        }
                    }

                    if ((bitoffset + noteoffset) % 6 == 0) // bit 다 채웠을 때
                    {
                        Fitness8 += StaticRepo.TableRepository.SavedTable.SequenceTable.patternsDevided[32 * bit[0] + 16 * bit[1] + 8 * bit[2] + 4 * bit[3] + 2 * bit[4] + 1 * bit[5]];

                        for(int i=0; i<6; i++)
                        {
                            bit[i] = 0;
                        }
                    }
                }
                
                bitoffset += noteoffset;
                baroffset += v.duration;

                if (baroffset >= 3840) // 만일 한 마디 넘어가면 루프 탈출!
                    break;
            }

            Fitness8 /= 2;

            #endregion

            #region Chord Table 에서 이어지는 노트에 대한 Fitness

            int count = 0;
            int nextcount = 0;
            int offset = 0;

            for (int i = 0; i < Length - 1; i++)
            {
                if (chords[offset] == chords[offset + (Note[i].duration / StaticRepo.ConfigRepository.TickPerMinNote)])
                {
                    nextcount = 1;
                    while (true)
                    {
                        if (i + nextcount == Length - 1)
                            break;

                        if (Note[i + nextcount].pitch == 0)
                            nextcount++;
                        else
                            break;
                    }

                    if (i + nextcount == Length - 1)
                        break;

                    Fitness2 += StaticRepo.TableRepository.ConvertedChordTable
                            [chords[offset].Index()] // 어떤 코드인지
                            [(Note[i].pitch % 12) + 1] // 해당 노트에 대한 점수를 얻는 열
                            [Note[i + nextcount].pitch % 12]; // 다음 노트의 점수
                    count++;
                }
                offset = offset + (Note[i].duration / StaticRepo.ConfigRepository.TickPerMinNote);
            }

            if (count == 0)
                Fitness2 = Fitness1;
            else
                Fitness2 /= count;

            #endregion

            #region 마디 하나에서 노트의 개수에 대한 Fitness

            Fitness3 = StaticRepo.TableRepository.SavedTable.SequenceTable.theNumberOfNotesDevided[Length - 1];

            #endregion

            #region 마디 하나에서 최고음과 최저음의 Pitch 차이에 대한 Fitness

            int betweenMax = 0;
            int betweenMin = 100;

            for (int i = 0; i < Length; i++)
            {
                if(Note[i].pitch != 0)
                {
                    if (Note[i].pitch > betweenMax)
                    {
                        betweenMax = Note[i].pitch;
                    }

                    if (Note[i].pitch < betweenMin)
                    {
                        betweenMin = Note[i].pitch;
                    }
                }
            }
            int between = betweenMax - betweenMin;

            if (between == -100) // 음표가 하나도 없으면 0점 처리 한다
            {
                Fitness4 = 0;
            }
            else if (between < StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow - 1)
            {
                Fitness4 = StaticRepo.TableRepository.SavedTable.SequenceTable.betweenHighAndLowDevided[between];
            }
            else
            {
                Fitness4 = StaticRepo.TableRepository.SavedTable.SequenceTable.betweenHighAndLowDevided[StaticRepo.ConfigRepository.SequenceTable_MaxBetweenHighAndLow - 1];
            }

            #endregion

            #region 마디 하나에서 변곡점의 개수에 대한 Fitness 와 각각의 노트 사이의 간격에 대한 Fitness

            int thisUpandDown = 0;
            int prevUpandDown = 0;
            int allChange = 0;

            for (int i = 0; i < Length - 1; i++)
            {
                if(Note[i].pitch != 0)
                {
                    thisUpandDown = Note[i].pitch - Note[i + 1].pitch;

                    if (Math.Abs(thisUpandDown) < 13)
                    {
                        Fitness6 += StaticRepo.TableRepository.SavedTable.SequenceTable.betweenNotesPitchDevided[Math.Abs(thisUpandDown)];
                    }
                    else
                    {
                        Fitness6 += StaticRepo.TableRepository.SavedTable.SequenceTable.betweenNotesPitchDevided[13];
                    }

                    if (thisUpandDown * prevUpandDown < 0)
                    {
                        allChange++;
                    }

                    prevUpandDown = thisUpandDown;
                }
            }

            Fitness5 = StaticRepo.TableRepository.SavedTable.SequenceTable.theNumberOfChangeDevided[allChange];

            Fitness6 /= (Length - 1);

            #endregion

            #region Note 들의 Length 에 대한 Fitness

            for (int i = 0; i < Length; i++)
            {
                Fitness7 += StaticRepo.TableRepository.SavedTable.SequenceTable.theNumberOfNoteLengthDevided[(Note[i].duration / StaticRepo.ConfigRepository.TickPerMinNote) - 1];
            }

            Fitness7 /= Length;

            #endregion

            Fitness1 *= StaticRepo.ConfigRepository.FitnessConstant_OneNoteChordTable;
            Fitness2 *= StaticRepo.ConfigRepository.FitnessConstant_TwoNoteChordTable;
            Fitness3 *= StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNotes;
            Fitness4 *= StaticRepo.ConfigRepository.FitnessConstant_BetweenHighAndLow;
            Fitness5 *= StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfChange;
            Fitness6 *= StaticRepo.ConfigRepository.FitnessConstant_BetweenNotesPitch;
            Fitness7 *= StaticRepo.ConfigRepository.FitnessConstant_TheNumberOfNoteLength;
            Fitness8 *= StaticRepo.ConfigRepository.FitnessConstant_IsFirstNoteInChord;
            Fitness9 *= StaticRepo.ConfigRepository.FitnessConstant_JazzRhythmScore;

            Fitness = Fitness1 + Fitness2 + Fitness3 + Fitness4 + Fitness5 + Fitness6 + Fitness7 + Fitness8 + Fitness9;

            Fitness_Detail[0] = Fitness;
            Fitness_Detail[1] = Fitness1;
            Fitness_Detail[2] = Fitness2;
            Fitness_Detail[3] = Fitness3;
            Fitness_Detail[4] = Fitness4;
            Fitness_Detail[5] = Fitness5;
            Fitness_Detail[6] = Fitness6;
            Fitness_Detail[7] = Fitness7;
            Fitness_Detail[8] = Fitness8;
            Fitness_Detail[9] = Fitness9;
        }

    }
}
