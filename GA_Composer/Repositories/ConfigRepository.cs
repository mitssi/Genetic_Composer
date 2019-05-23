using GA_Composer.ViewModels;
using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GA_Composer.Repositories
{
    public class ConfigRepository
    {
        public Random GlobalRandom { get; set; } = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// 현재 컴퓨터가 선택한 Output Device
        /// </summary>
        public OutputDevice MainOutputDevice { get; set; } = OutputDevice.InstalledDevices[0];

        /// <summary>
        /// 한 마디 안에 몇 틱일지
        /// </summary>
        public int TickPerOneBar { get; set; } = 3840;

        /// <summary>
        /// 한 마디 안에 들어가는 음표의 최대 개수
        /// </summary>
        public int MaxNumOfNote { get; set; } = 12;

        public int TickPerMinNote { get; set; }

        private double rouletteK = 4;

        /// <summary>
        /// 마디 개수
        /// </summary>
        public int NumberOfBar { get; set; } = 16;

        /// <summary>
        /// 룰렛 휠 상수
        /// </summary>
        public double RouletteK
        {
            get
            {
                return rouletteK;
            }
            set
            {
                rouletteK = value;
                StaticVM.MainViewModel.Moniter_rouletteK.Text = value.ToString();
            }
        }

        private int generation = 1;
        /// <summary>
        /// 현재 세대 수
        /// </summary>
        public int Generation
        {
            get
            {
                return generation;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_generation.Text = value.ToString();
                generation = value;
            }
        }
        
        private int population = 100;
        /// <summary>
        /// Population
        /// </summary>
        public int Population
        {
            get
            {
                return population;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_population.Text = value.ToString();
                population = value;
            }
        }

        private double bestFitness;
        /// <summary>
        /// 가장 좋은 Fitness 를 모두 더한 것
        /// </summary>
        public double BestFitness
        {
            get
            {
                return bestFitness;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_bestfitness.Text = Math.Round(value, 3).ToString();
                bestFitness = value;
            }
        }

        private double averageFitness;
        /// <summary>
        /// 평균 Fitness 를 모두 더한 것
        /// </summary>
        public double AverageFitness
        {
            get
            {
                return averageFitness;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_averagefitness.Text = Math.Round(value, 3).ToString();
                averageFitness = value;
            }
        }

        private double mutation = 0.1;
        /// <summary>
        /// 변이 확률
        /// </summary>
        public double Mutation
        {
            get
            {
                return mutation;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_mutation.Text = Math.Round(value, 3).ToString();
                mutation = value;
            }
        }

        private double legatopercentage = 0.5;
        /// <summary>
        /// 변이 확률
        /// </summary>
        public double Legatopercentage
        {
            get
            {
                return legatopercentage;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_legatopercentage.Text = Math.Round(value, 3).ToString();
                legatopercentage = value;
            }
        }

        private int elitism = 1;
        /// <summary>
        /// 변이 확률
        /// </summary>
        public int Elitism
        {
            get
            {
                return elitism;
            }
            set
            {
                StaticVM.MainViewModel.Moniter_elitism.Text = value.ToString();
                elitism = value;
            }
        }

        public int SequenceTable_MaxBetweenHighAndLow { get; set; } = 20;

        public int SequenceTable_MaxBetweenNotesPitch { get; set; } = 14;
        
        public int SequenceTable_MaxPatterns { get; set; } = 64;

        /// <summary>
        /// 상수1. 코드 테이블에서 노트 하나의 점수
        /// </summary>
        public double FitnessConstant_OneNoteChordTable { get; set; }

        /// <summary>
        /// 상수2. 코드 테이블에서 이어지는 노트에 대한 점수
        /// </summary>
        public double FitnessConstant_TwoNoteChordTable { get; set; }

        /// <summary>
        /// 상수3. 마디 하나에서 노트의 개수
        /// </summary>
        public double FitnessConstant_TheNumberOfNotes { get; set; }

        /// <summary>
        /// 상수4. 마디 하나에서 최고음과 최저음의 Pitch 차이
        /// </summary>
        public double FitnessConstant_BetweenHighAndLow { get; set; }

        /// <summary>
        /// 상수5. 마디 하나에서 변곡점의 개수
        /// </summary>
        public double FitnessConstant_TheNumberOfChange { get; set; }
        
        /// <summary>
        /// 상수6. Pitch 2개 사이의 간격
        /// </summary>
        public double FitnessConstant_BetweenNotesPitch { get; set; }

        /// <summary>
        /// 상수7. Note 의 Duration 들의 모음
        /// </summary>
        public double FitnessConstant_TheNumberOfNoteLength { get; set; }

        /// <summary>
        /// 상수8. 코드에서 맨 처음 노트가 코드 구성음인지
        /// </summary>
        public double FitnessConstant_IsFirstNoteInChord { get; set; }

        /// <summary>
        /// 상수9. 재즈 리듬 스코어
        /// </summary>
        public double FitnessConstant_JazzRhythmScore { get; set; }

        public Scale thisSclae = new Scale(new Note("C"), Scale.Ionian);

    }
}
