using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Yahtzee
{
    public partial class Form1 : Form
    {
        Random generator = new Random();
        List<string> dicesSources;
        List<string> dicesSelectedSources;
        List<Label> userLabels = new List<Label>();
        List<Label> machineLabels = new List<Label>();
        List<PictureBox> dices = new List<PictureBox>();
        List<PictureBox> dicesHeldDown = new List<PictureBox>();
        List<int> finalNumbers = new List<int>();
        int diceIndex;
        int userRoundCount;
        int machineRoundCount;
        int userTotal;
        int machineTotal;
        System.Media.SoundPlayer soundPlayer;
        Timer machineTimer;
        Timer roundEnd;
        Button rollingButton;
        Tuple<string, List<int>> machineRoundChoice = null;
        bool machineRoundEnd = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackgroundImage = Image.FromFile("../../Pictures/yahtzeebackground2.png");
            BackgroundImageLayout = ImageLayout.Stretch;

            dicesSources = Directory.GetFiles("../../Pictures/Dices").ToList();
            dicesSelectedSources = Directory.GetFiles("../../Pictures/Dices/Dices").ToList();

            foreach (PictureBox pb in Controls.OfType<PictureBox>())
            {
                dices.Add(pb);
            }
            foreach (Label l in Controls.OfType<Label>())
            {
                if (l.Name.StartsWith("user_"))
                {
                    userLabels.Add(l);
                    l.DoubleClick += UserLabel_DoubleClick;
                }
                else if (l.Name.StartsWith("machine_"))
                {
                    machineLabels.Add(l);
                }
            }

            rollingButton = (Button)Controls.Find("rollButton", false)[0];

            userTotal = 0;
            machineTotal = 0;
            Play();
        }

        void Play()
        {
            dicesToSaveLabel.Visible = false;
            roundResultLabel.Visible = false;

            if (userRoundCount < 3)
            {
                rollingButton.Click += RollButton_Click;
            }
            else
            {
                rollingButton.Click -= RollButton_Click;
                ShowFinalPoints();
            }
        }

        void RollingDices(PictureBox dice)
        {
            soundPlayer = new SoundPlayer();
            soundPlayer.SoundLocation = "rolldice.wav";
            soundPlayer.Play();

            diceIndex = generator.Next(0, dicesSources.Count);
            dice.Image = Image.FromFile(dicesSources[diceIndex]);
            dice.ImageLocation = dicesSources[diceIndex];
            dice.Name = dice.ImageLocation.Split('/').Last().Split('.').First().Last().ToString();
        }

        void MachineRound()
        {
            dicesToSaveLabel.Visible = false;
            machineRoundCount++;
            if (machineRoundCount <= 3 && machineRoundEnd == false)
            {
                machineTimer = new Timer();
                machineTimer.Interval = 3000;
                machineTimer.Enabled = true;
                machineTimer.Start();
                machineTimer.Tick += MachineRounds_Tick;
            }
            else
            {
                finalNumbers.Clear();
                finalNumbers = dices.Where(x => dicesSelectedSources.Contains(x.ImageLocation)).ToList()
                               .Select(x => Convert.ToInt32(x.Name)).ToList();

                CalculateMachineRoundPoint();

                machineRoundChoice = null;
                machineRoundCount = 0;
                machineRoundEnd = false;

                machineTimer = new Timer();
                machineTimer.Interval = 3000;
                machineTimer.Enabled = true;
                machineTimer.Start();
                machineTimer.Tick += MachineRoundEnd_Tick;
            }
        }

        void MachineRoundEnd_Tick(object sender, EventArgs e)
        {
            if(machineLabels.Any(x=>CheckIfLabelIsEmpty(x)))
            {
                ClearRound();
                EndMachineTimer();
                Play();
            }
            else
            {
                ClearRound();
                EndMachineTimer();
                GameOver();
            }
        }

        void GameOver()
        {
            if(userTotal > machineTotal)
            {
                MessageBox.Show("Congratulations, you won, well played game!");
            }
            else
            {
                MessageBox.Show("Sorry, the machine won, better luck for next time!");
            }
        }

        void CalculateMachineRoundPoint()
        {
            List<Label> emptyLabels = machineLabels.Where(x => CheckIfLabelIsEmpty(x)).ToList();

            if (NumbersVsMachineChoice(machineRoundChoice.Item1, finalNumbers))
            {
                Label machineLabel = emptyLabels.Single(x => ChangeLabelName(x) == machineRoundChoice.Item1);
                FillMachineLabelWithPoint(machineLabel, CalculatePoints(machineRoundChoice.Item1, finalNumbers));
            }
            else
            {
                finalNumbers = dices.Select(x => Convert.ToInt32(x.Name)).ToList();

                List<Tuple<string, Tuple<List<int>, double>>> machineLabelsChances = MachineLabelsChances(dices);
                List<List<Tuple<string, Tuple<List<int>, double>>>> listsMachineLabelsChances = SplitMachineLabelsChances(machineLabelsChances);

                List<Tuple<string, Tuple<List<int>, double>>> fixedChoices = listsMachineLabelsChances[0];
                List<Tuple<string, Tuple<List<int>, double>>> duplicateChoices = listsMachineLabelsChances[1];
                List<Tuple<string, Tuple<List<int>, double>>> numberChoices = listsMachineLabelsChances[2];

                List<int> duplicatedNums = finalNumbers.Where(x => finalNumbers.Count(y => y == x) > 1).ToList();

                Tuple<string, Tuple<List<int>, double>> newChoice;

                if (fixedChoices.Any(x => x.Item2.Item2 == 100))
                {
                    int maxPoint = fixedChoices.Select(x => CalculatePoints(x.Item1, finalNumbers)).Max(x => x);
                    newChoice = fixedChoices.Where(x => x.Item2.Item2 == 100).Single(x => CalculatePoints(x.Item1, finalNumbers) == maxPoint);

                    FillFinalMachinePoint(newChoice);
                }
                else if (duplicateChoices.Any(x => x.Item2.Item2 == 100))
                {
                    List<Tuple<string, Tuple<List<int>, double>>> duplicates = duplicateChoices.Where(x => x.Item2.Item2 == 100).ToList();

                    if (duplicates.Any(x => CompareToMaximum(x.Item1, CalculatePoints(x.Item1, finalNumbers))))
                    {
                        List<Tuple<string, Tuple<List<int>, double>>> newChoices =
                        duplicates.Where(x => CompareToMaximum(x.Item1, CalculatePoints(x.Item1, finalNumbers))).ToList();

                        int maxPoint = newChoices.Max(y => CalculatePoints(y.Item1, finalNumbers));
                        newChoice = newChoices.Where(x => CalculatePoints(x.Item1, finalNumbers) == maxPoint)
                                    .Where(x => MaxPointLabels(x.Item1) == (newChoices.Max(y => MaxPointLabels(y.Item1))))
                                    .FirstOrDefault();

                        FillFinalMachinePoint(newChoice);
                    }
                    else
                    {
                        if (numberChoices.Count > 0)
                        {
                            if (machine_Chance.Text == "" && CompareToMaximum("Chance", finalNumbers.Sum()))
                            {
                                newChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(finalNumbers, 0));
                                FillFinalMachinePoint(newChoice);
                            }
                            else
                            {
                                List<Tuple<string, Tuple<List<int>, double>>> newChoices =
                                numberChoices.Where(x => x.Item2.Item2 == (numberChoices.Max(y => y.Item2.Item2))).ToList();
                                int minPoint = newChoices.Min(y => CalculatePoints(y.Item1, finalNumbers));
                                newChoice = newChoices.Single(x => CalculatePoints(x.Item1, finalNumbers) == minPoint);
                                FillFinalMachinePoint(newChoice);
                            }
                        }
                        else
                        {
                            if (machine_Chance.Text == "" && CompareToMaximum("Chance", finalNumbers.Sum()))
                            {
                                newChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(finalNumbers, 0));
                                FillFinalMachinePoint(newChoice);
                            }
                            else
                            {
                                int maxPoint = duplicates.Max(y => CalculatePoints(y.Item1, finalNumbers));
                                newChoice = duplicates.Single(x => CalculatePoints(x.Item1, finalNumbers) == maxPoint);
                                FillFinalMachinePoint(newChoice);
                            }
                        }
                    }
                }
                else if (numberChoices.Count > 0)
                {
                    if (numberChoices.Any(x => CompareToMaximum(x.Item1, CalculatePoints(x.Item1, finalNumbers))))
                    {
                        newChoice = numberChoices.Single(x => CompareToMaximum(x.Item1, CalculatePoints(x.Item1, finalNumbers)));
                        FillFinalMachinePoint(newChoice);
                    }
                    else
                    {
                        if (machine_Chance.Text == "" && CompareToMaximum("Chance", finalNumbers.Sum()))
                        {
                            newChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(finalNumbers, 100));
                            FillFinalMachinePoint(newChoice);
                        }
                        else
                        {
                            List<Tuple<string, Tuple<List<int>, double>>> newChoices =
                            numberChoices.Where(x => x.Item2.Item2 == (numberChoices.Max(y => y.Item2.Item2))).ToList();
                            int minPoint = newChoices.Min(y => CalculatePoints(y.Item1, finalNumbers));
                            newChoice = newChoices.Single(x => CalculatePoints(x.Item1, finalNumbers) == minPoint);
                            FillFinalMachinePoint(newChoice);
                        }
                    }
                }
                else
                {
                    if (machine_Chance.Text == "")
                    {
                        newChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(finalNumbers, 0));
                        FillFinalMachinePoint(newChoice);
                    }
                    else
                    {
                        List<Label> finalEmptyLabels = machineLabels.Where(x => x.Text == "").ToList();
                        int maxPointLabel = finalEmptyLabels.Max(y => MaxPointLabels(ChangeLabelName(y)));
                        Label newChoiceName = finalEmptyLabels.Where(x => MaxPointLabels(ChangeLabelName(x)) == maxPointLabel).FirstOrDefault();
                        newChoiceName.Text = "0";
                        newChoiceName.BackColor = Color.Lime;
                    }
                }
            }
        }

        int MaxPointLabels(string choiceName)
        {
            int numToReturn;
            switch (choiceName)
            {
                case "Aces": numToReturn = 5; break;
                case "Twos": numToReturn = 10; break;
                case "Threes": numToReturn = 15; break;
                case "Fours": numToReturn = 20; break;
                case "Fives": numToReturn = 25; break;
                case "Sixes": numToReturn = 30; break;
                case "Small Straight": numToReturn = 30; break;
                case "Large Straight": numToReturn = 40; break;
                case "Yahtzee": numToReturn = 50; break;
                case "Full House": numToReturn = 25; break;
                case "Chance": numToReturn = 30; break;
                case "Drill": numToReturn = 18; break;
                case "Two Pairs": numToReturn = 22; break;
                case "Pair": numToReturn = 12; break;
                case "Poker": numToReturn = 24; break;
                default: numToReturn = 0; break;
            }
            return numToReturn;
        }

        void FillFinalMachinePoint(Tuple<string, Tuple<List<int>, double>> newChoice)
        {
            List<Label> emptyLabels = machineLabels.Where(x => CheckIfLabelIsEmpty(x)).ToList();
            machineRoundChoice = new Tuple<string, List<int>>(newChoice.Item1, finalNumbers);
            Label machineLabel = emptyLabels.Single(x => ChangeLabelName(x) == machineRoundChoice.Item1);
            FillMachineLabelWithPoint(machineLabel, CalculatePoints(machineRoundChoice.Item1, finalNumbers));
        }

        bool NumbersVsMachineChoice(string choiceName, List<int> numbers)
        {
            numbers = finalNumbers;
            if (NumberLines(choiceName))
            {
                return finalNumbers.Select(x => ReturnNumberLine(x)).ToList().Contains(choiceName);
            }
            else
            {
                bool boolToReturn;
                switch (choiceName)
                {
                    case "Drill": boolToReturn = CheckIfDrill(numbers); break;
                    case "Two Pairs": boolToReturn = CheckIfTwoPairs(numbers); break;
                    case "Pair": boolToReturn = CheckIfOnePair(numbers); break;
                    case "Small Straight": boolToReturn = CheckIfSmallStraight(numbers); break;
                    case "Large Straight": boolToReturn = CheckIfLargeStraight(numbers); break;
                    case "Full House": boolToReturn = CheckIfFullHouse(numbers); ; break;
                    case "Poker": boolToReturn = CheckIfPoker(numbers); break;
                    case "Yahtzee": boolToReturn = CheckIfYahtzee(numbers); break;
                    default: boolToReturn = false; break;
                }
                return boolToReturn;
            }
        }

        void MachineRounds_Tick(object sender, EventArgs e)
        {
            foreach (PictureBox pb in dices)
            {
                if (!dicesSelectedSources.Contains(pb.ImageLocation))
                {
                    RollingDices(pb);
                }
            }
            DecideMachineRound(dices);
        }

        void EndMachineTimer()
        {
            machineTimer.Enabled = false;
            machineTimer.Stop();
            machineTimer.Dispose();
        }

        List<Tuple<string, Tuple<List<int>, double>>> MachineLabelsChances(List<PictureBox> dicesList)
        {
            List<int> roundNumbers = dicesList.Select(x => Convert.ToInt32(x.Name)).ToList();

            List<Label> emptyLabels = machineLabels.Where(x => CheckIfLabelIsEmpty(x)).ToList();

            List<Tuple<string, Tuple<List<int>, double>>> machineLabelsChances = new List<Tuple<string, Tuple<List<int>, double>>>();
            foreach (Label l in emptyLabels)
            {
                Tuple<string, Tuple<List<int>, double>> elementChance = CalculateMachineChancePerItem(roundNumbers, ChangeLabelName(l));
                if (elementChance.Item2.Item1 != null)
                {
                    machineLabelsChances.Add(elementChance);
                }
            }
            return machineLabelsChances;
        }

        List<List<Tuple<string, Tuple<List<int>, double>>>> SplitMachineLabelsChances(List<Tuple<string, Tuple<List<int>, double>>> machineLabelsChances)
        {
            List<Tuple<string, Tuple<List<int>, double>>> fixedChoices = new List<Tuple<string, Tuple<List<int>, double>>>();
            List<Tuple<string, Tuple<List<int>, double>>> duplicateChoices = new List<Tuple<string, Tuple<List<int>, double>>>();
            List<Tuple<string, Tuple<List<int>, double>>> numberChoices = new List<Tuple<string, Tuple<List<int>, double>>>();
            foreach (Tuple<string, Tuple<List<int>, double>> choice in machineLabelsChances)
            {
                if (FixedPoint(choice.Item1))
                {
                    fixedChoices.Add(choice);
                }
                else if (DuplicateNums(choice.Item1))
                {
                    duplicateChoices.Add(choice);
                }
                else
                {
                    numberChoices.Add(choice);
                }
            }

            List<List<Tuple<string, Tuple<List<int>, double>>>> listsMachineLabelsChances
            = new List<List<Tuple<string, Tuple<List<int>, double>>>>() { fixedChoices, duplicateChoices, numberChoices };

            return listsMachineLabelsChances;
        }

        void DecideMachineRound(List<PictureBox> dicesList)
        {
            List<Tuple<string, Tuple<List<int>, double>>> machineLabelsChances = MachineLabelsChances(dicesList);
            List<List<Tuple<string, Tuple<List<int>, double>>>> listsMachineLabelsChances = SplitMachineLabelsChances(machineLabelsChances);

            List<Tuple<string, Tuple<List<int>, double>>> fixedChoices = listsMachineLabelsChances[0];
            List<Tuple<string, Tuple<List<int>, double>>> duplicateChoices = listsMachineLabelsChances[1];
            List<Tuple<string, Tuple<List<int>, double>>> numberChoices = listsMachineLabelsChances[2];

            Tuple<string, Tuple<List<int>, double>> machineChoice = null;

            if (fixedChoices.Count > 0)
            {
                List<Tuple<string, Tuple<List<int>, double>>> newList = fixedChoices.Where(x => x.Item2.Item2 == fixedChoices.Max(y => y.Item2.Item2)).ToList();
                machineChoice = newList.OrderByDescending(x => CalculatePoints(x.Item1, x.Item2.Item1)).FirstOrDefault();
            }
            else if (duplicateChoices.Count > 0)
            {
                List<Tuple<string, int>> namesExpectedPoints = new List<Tuple<string, int>>();
                foreach (Tuple<string, Tuple<List<int>, double>> choice in duplicateChoices)
                {
                    Tuple<string, int> tuple = new Tuple<string, int>(choice.Item1, MachineExpectedPoint(choice.Item1, choice.Item2.Item1));
                    namesExpectedPoints.Add(tuple);
                }
                string maxName = namesExpectedPoints.Where(x => x.Item2 == namesExpectedPoints.Max(y => y.Item2))
                                .Select(x => x.Item1).FirstOrDefault();
                machineChoice = duplicateChoices.Single(x => x.Item1 == maxName);

            }
            else if (numberChoices.Count > 0)
            {
                if (numberChoices.Count > 1)
                {
                    string maxName = numberChoices.Where(x => x.Item2.Item1.FirstOrDefault()
                                   == numberChoices.Max(y => y.Item2.Item1.FirstOrDefault()))
                                   .Select(x => x.Item1).FirstOrDefault();

                    if (maxName != null)
                    {
                        machineChoice = numberChoices.Single(x => x.Item1 == maxName);
                    }
                    else
                    {
                        List<Label> emptyNumChoices = machineLabels.Where(x => CheckIfLabelIsEmpty(x)).ToList();
                        Label finalChoice = emptyNumChoices.Where(x => MaxPointLabels(ChangeLabelName(x))
                                                 == emptyNumChoices.Max(y => MaxPointLabels(ChangeLabelName(y))))
                                                 .FirstOrDefault();

                        List<int> roundNums = dices.Select(x => Convert.ToInt32(x.Name)).ToList();

                        machineChoice = new Tuple<string, Tuple<List<int>, double>> 
                        (ChangeLabelName(finalChoice), new Tuple<List<int>, double>(roundNums, 0));
                    }
                }
                else
                {
                    Tuple<string, Tuple<List<int>, double>> lastEmptyNum = numberChoices.FirstOrDefault();
                    List<int> roundNums = dices.Select(x => Convert.ToInt32(x.Name)).ToList();
                    double lastItemChance = (roundNums.Count(x => x == ReturnNumber(lastEmptyNum.Item1)) / roundNums.Count) * 100;

                    machineChoice = new Tuple<string, Tuple<List<int>, double>>
                    (lastEmptyNum.Item1, new Tuple<List<int>, double> (roundNums.Where(x=>x == ReturnNumber(lastEmptyNum.Item1)).ToList(), lastItemChance));
                }
            }
            else if (machine_Chance.Text == "")
            {
                List<int> roundNums = dices.Select(x => Convert.ToInt32(x.Name)).ToList();
                if (CompareToMaximum("Chance", roundNums.Sum()))
                {
                    machineChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(roundNums, 100));
                }
                else
                {
                    List<int> maxRoundNums = roundNums.Where(x => x > 3).ToList();
                    machineChoice = new Tuple<string, Tuple<List<int>, double>>("Chance", new Tuple<List<int>, double>(roundNums, (maxRoundNums.Count / 5) * 100));
                }
            }

            if (machineRoundChoice == null)
            {
                ChangeLabelColorInMachineRound(dices, machineChoice.Item1, machineChoice.Item2.Item1);

                List<int> roundNums = new List<int>();
                foreach (PictureBox pb in dices)
                {
                    if (dicesSelectedSources.Contains(pb.ImageLocation))
                    {
                        roundNums.Add(Convert.ToInt32(pb.Name));
                    }
                }

                machineRoundChoice = new Tuple<string, List<int>>(machineChoice.Item1, roundNums);

                if (machineChoice.Item2.Item2 == 100)
                {
                    machineRoundEnd = true;
                    ChangeLabelColorInMachineRound(dices, machineRoundChoice.Item1, machineRoundChoice.Item2);
                    EndMachineTimer();
                    MachineRound();
                }
                else
                {
                    EndMachineTimer();
                    MachineRound();
                }
            }
            else
            {
                double choiceChance = machineLabelsChances.Single(x => x.Item1 == machineRoundChoice.Item1).Item2.Item2;

                if (choiceChance == 100)
                {
                    List<int> choiceNums = machineLabelsChances.Single(x => x.Item1 == machineRoundChoice.Item1).Item2.Item1;
                    machineRoundEnd = true;
                    ChangeLabelColorInMachineRound(dices, machineRoundChoice.Item1, choiceNums);
                    EndMachineTimer();
                    MachineRound();
                }
                else
                {
                    if (machineChoice != null)
                    {
                        if (machineChoice.Item1 == machineRoundChoice.Item1)
                        {
                            UnchangedChoice(machineLabelsChances);
                        }
                        else
                        {
                            double newChoiceChance = machineLabelsChances.Single(x => x.Item1 == machineChoice.Item1).Item2.Item2;
                            if (choiceChance > newChoiceChance)
                            {
                                UnchangedChoice(machineLabelsChances);
                            }
                            else
                            {
                                List<int> choiceList = machineLabelsChances.Single(x => x.Item1 == machineChoice.Item1).Item2.Item1;
                                ChangeLabelColorInMachineRound(dices, machineChoice.Item1, choiceList);
                                machineRoundChoice = new Tuple<string, List<int>>(machineChoice.Item1, choiceList);

                                EndMachineTimer();
                                MachineRound();
                            }
                        }
                    }
                    else
                    {
                        UnchangedChoice(machineLabelsChances);
                    }
                }
            }
        }

        void UnchangedChoice(List<Tuple<string, Tuple<List<int>, double>>> machineLabelsChances)
        {
            List<int> choiceList = machineLabelsChances.Single(x => x.Item1 == machineRoundChoice.Item1).Item2.Item1;
            machineRoundChoice = new Tuple<string, List<int>>(machineRoundChoice.Item1, choiceList);
            ChangeLabelColorInMachineRound(dices, machineRoundChoice.Item1, choiceList);

            EndMachineTimer();
            MachineRound();
        }

        void ChangeLabelColorInMachineRound(List<PictureBox> roundDices, string choiceName, List<int> roundNums)
        {
            if (NumberLines(choiceName) || DuplicateNums(choiceName))
            {
                foreach (PictureBox pb in roundDices)
                {
                    if (!dicesSelectedSources.Contains(pb.ImageLocation))
                    {
                        if (roundNums.Contains(Convert.ToInt32(pb.Name)))
                        {
                            ChangeDiceColor(pb, dicesSelectedSources);
                        }
                    }
                    else
                    {
                        if (!roundNums.Contains(Convert.ToInt32(pb.Name)))
                        {
                            ChangeDiceColor(pb, dicesSources);
                        }
                    }
                }
            }
            else
            {
                if (choiceName == "Small Straight" || choiceName == "Large Straight")
                {
                    List<PictureBox> neededDices = new List<PictureBox>();
                    foreach (PictureBox pb in roundDices)
                    {
                        if (roundNums.Contains(Convert.ToInt32(pb.Name)))
                        {
                            if (!neededDices.Any(x => x.Name == pb.Name))
                            {
                                neededDices.Add(pb);
                            }
                            else
                            {
                                ChangeDiceColor(pb, dicesSources);
                            }
                        }
                        else
                        {
                            ChangeDiceColor(pb, dicesSources);
                        }
                    }

                    foreach (PictureBox pb in neededDices)
                    {
                        if (!dicesSelectedSources.Contains(pb.ImageLocation))
                        {
                            ChangeDiceColor(pb, dicesSelectedSources);
                        }
                    }
                }
                else
                {
                    foreach (PictureBox pb in roundDices)
                    {
                        if (!dicesSelectedSources.Contains(pb.ImageLocation))
                        {
                            if (roundNums.Contains(Convert.ToInt32(pb.Name)))
                            {
                                ChangeDiceColor(pb, dicesSelectedSources);
                            }
                        }
                        else
                        {
                            if (!roundNums.Contains(Convert.ToInt32(pb.Name)))
                            {
                                ChangeDiceColor(pb, dicesSources);
                            }
                        }
                    }
                }
            }
        }

        bool FixedPoint(string labelName)
        {
            return labelName == "Full House" || labelName == "Small Straight" || labelName == "Large Straight" || labelName == "Yahtzee";

        }

        bool DuplicateNums(string labelName)
        {
            return labelName == "Drill" || labelName == "Pair" || labelName == "Two Pairs" || labelName == "Poker";
        }

        bool NumberLines(string labelName)
        {
            return labelName == "Ones" || labelName == "Twos" || labelName == "Threes" || labelName == "Fours" || labelName == "Fives" || labelName == "Sixes";
        }

        int MachineExpectedPoint(string choiceName, List<int> choiceNums)
        {
            if (NumberLines(choiceName))
            {
                return choiceNums.Distinct().FirstOrDefault() * 5;
            }
            else if (choiceName == "Drill")
            {
                return choiceNums.FirstOrDefault() * 3;
            }
            else if (choiceName == "Two Pairs")
            {
                if (choiceNums.Distinct().Count() >= 2)
                {
                    return (choiceNums.Distinct().ToList()[0] * 2) + (choiceNums.Distinct().ToList()[1] * 2);
                }
                else
                {
                    return (choiceNums.Distinct().ToList()[0] * 2);
                }
            }
            else if (choiceName == "Poker")
            {
                return choiceNums.FirstOrDefault() * 4;
            }
            else if (choiceName == "Pair")
            {
                return choiceNums.FirstOrDefault() * 2;
            }
            return 0;
        }

        bool CompareToMaximum(string choiceName, int expectedPoint)
        {
            bool boolToReturn;
            switch (choiceName)
            {
                case "Aces": boolToReturn = (expectedPoint) > (5 / 2); break;
                case "Twos": boolToReturn = (expectedPoint) > (10 / 2); break;
                case "Threes": boolToReturn = (expectedPoint) > (15 / 2); break;
                case "Fours": boolToReturn = (expectedPoint) > (20 / 2); break;
                case "Fives": boolToReturn = (expectedPoint) > (25 / 2); break;
                case "Sixes": boolToReturn = (expectedPoint) > (30 / 2); break;
                case "Drill": boolToReturn = (expectedPoint) > (18 / 2); break;
                case "Two Pairs": boolToReturn = (expectedPoint) > (22 / 2); break;
                case "Pair": boolToReturn = (expectedPoint) > 8; break;
                case "Poker": boolToReturn = (expectedPoint) > (24 / 2); break;
                case "Chance": boolToReturn = (expectedPoint) > 20; break;
                default: boolToReturn = false; break;
            }
            return boolToReturn;
        }

        Tuple<string, Tuple<List<int>, double>> CalculateMachineChancePerItem(List<int> roundNumbers, string machineLabel)
        {
            Tuple<string, Tuple<List<int>, double>> tupleToReturn;
            switch (machineLabel)
            {
                case "Aces": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 1)); break;
                case "Twos": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 2)); break;
                case "Threes": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 3)); break;
                case "Fours": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 4)); break;
                case "Fives": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 5)); break;
                case "Sixes": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckNumbers(roundNumbers, 6)); break;
                case "Large Straight": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckStraights(roundNumbers, machineLabel)); break;
                case "Small Straight": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckStraights(roundNumbers, machineLabel)); break;
                case "Pair": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDuplicates(roundNumbers, 2)); break;
                case "Drill": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDuplicates(roundNumbers, 3)); break;
                case "Poker": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDuplicates(roundNumbers, 4)); break;
                case "Yahtzee": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDuplicates(roundNumbers, 5)); break;
                case "Full House": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDoublePairs(roundNumbers, machineLabel)); break;
                case "Two Pairs": tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(machineLabel, MachineCheckDoublePairs(roundNumbers, machineLabel)); break;
                default: tupleToReturn = new Tuple<string, Tuple<List<int>, double>>(null, new Tuple<List<int>, double>(null, 0)); break;
            }
            return tupleToReturn;
        }

        Tuple<List<int>, double> MachineCheckNumbers(List<int> roundNumbers, int number)
        {
            List<int> numberLine = new List<int>() { number, number, number, number, number };

            List<int> chosenDicesNums = new List<int>();
            int commonRoundNumLine = 0;
            foreach (int num in roundNumbers)
            {
                int index = roundNumbers.IndexOf(num);
                int lineNum = numberLine.ElementAt(index);
                if (num == lineNum)
                {
                    chosenDicesNums.Add(num);
                    commonRoundNumLine++;
                }
            }

            if (commonRoundNumLine > 0)
            {
                Tuple<List<int>, double> newTuple = new Tuple<List<int>, double>(chosenDicesNums, (((double)(commonRoundNumLine * 10) / (double)(roundNumbers.Count * 10)) * 100));
                return newTuple;
            }
            else
            {
                Tuple<List<int>, double> newTuple = new Tuple<List<int>, double>(null, 0);
                return newTuple;
            }
        }

        Tuple<List<int>, double> MachineCheckStraights(List<int> roundNumbers, string labelName)
        {
            List<int> roundNumbersOrder = roundNumbers.OrderBy(x => x).ToList();

            List<int> straight1 = new List<int>() { 1, 2, 3, 4, 5 };
            List<int> straight2 = new List<int>() { 2, 3, 4, 5, 6 };

            List<int> commonWithRoundLarge1 = roundNumbersOrder.Intersect(straight1).ToList();
            List<int> commonWithRoundLarge2 = roundNumbersOrder.Intersect(straight2).ToList();

            List<int> commonWithRoundSmall1 = roundNumbersOrder.Intersect(straight1.Take(4).ToList()).ToList();
            List<int> commonWithRoundSmall2 = roundNumbersOrder.Intersect(straight2.Take(4).ToList()).ToList();
            List<int> commonWithRoundSmall3 = roundNumbersOrder.Intersect(straight2.Skip(1).ToList()).ToList();

            List<List<int>> commonsLarge = new List<List<int>>() { commonWithRoundLarge1, commonWithRoundLarge2 };
            List<List<int>> commonsSmall = new List<List<int>>() { commonWithRoundSmall1, commonWithRoundSmall2, commonWithRoundSmall3 };

            if (labelName == "Large Straight")
            {
                if (commonsLarge.Max(x => x.Count) > 0)
                {
                    return new Tuple<List<int>, double>(commonsLarge.Where(x => x.Count == commonsLarge.Max(y => y.Count)).FirstOrDefault(),
                                                       ((double)(commonsLarge.Max(x => x.Count) * 10) / (double)(roundNumbers.Count * 10)) * 100);
                }
                else
                {
                    return new Tuple<List<int>, double>(null, 0);
                }
            }
            else
            {
                if (commonsSmall.Max(x => x.Count) > 0)
                {
                    return new Tuple<List<int>, double>(commonsSmall.Where(x => x.Count == commonsSmall.Max(y => y.Count)).FirstOrDefault(),
                                                       ((double)(commonsSmall.Max(x => x.Count) * 10) / (double)((roundNumbers.Count - 1) * 10)) * 100);
                }
                else
                {
                    return new Tuple<List<int>, double>(null, 0);
                }
            }
        }

        Tuple<List<int>, double> MachineCheckDuplicates(List<int> roundNumbers, int duplicates)
        {
            List<int> numbers = new List<int>() { 1, 2, 3, 4, 5, 6 };

            List<Tuple<int, int>> numbersChances = new List<Tuple<int, int>>();
            foreach (int num in numbers)
            {
                Tuple<int, int> newTuple = new Tuple<int, int>(num, Convert.ToInt32(MachineCheckNumbers(roundNumbers, num).Item2));
                numbersChances.Add(newTuple);
            }

            int maxChance = numbersChances.Max(x => x.Item2);
            Tuple<int, int> maxChanceNum = numbersChances.Where(x => x.Item2 == maxChance).ToList().OrderByDescending(y => y.Item1).First();

            if (duplicates == 2)
            {
                if (maxChanceNum.Item2 == 20)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(10) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 >= 40)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, 100); ;
                }
            }
            else if (duplicates == 3)
            {
                if (maxChanceNum.Item2 == 20)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(10) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 40)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(20) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 >= 60)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, 100);
                }
            }
            else if (duplicates == 4)
            {
                if (maxChanceNum.Item2 == 20)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(10) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 40)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(20) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 60)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(30) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 >= 80)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, 100);
                }
            }
            else if (duplicates == 5)
            {
                if (maxChanceNum.Item2 == 20)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(10) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 40)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(20) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 60)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(30) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 80)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, (((double)(40) / (double)(duplicates * 10)) * 100));
                }
                else if (maxChanceNum.Item2 == 100)
                {
                    return new Tuple<List<int>, double>(new List<int>() { maxChanceNum.Item1 }, 100);
                }
            }
            return null;
        }

        Tuple<List<int>, double> MachineCheckDoublePairs(List<int> roundNumbers, string labelName)
        {
            List<int> roundNumsDistinctList = roundNumbers.Distinct().ToList().OrderByDescending(x => x).ToList();
            int roundNumsDistinct = roundNumsDistinctList.Count;

            List<int> numbers = new List<int>() { 1, 2, 3, 4, 5, 6 };

            List<Tuple<int, int>> numbersChances = new List<Tuple<int, int>>();
            foreach (int num in numbers)
            {
                Tuple<int, int> newTuple = new Tuple<int, int>(num, Convert.ToInt32(MachineCheckNumbers(roundNumbers, num).Item2));
                numbersChances.Add(newTuple);
            }

            List<int> pair = numbersChances.Where(x => x.Item2 >= 40).ToList().OrderByDescending(x => x.Item1).Select(x => x.Item1).ToList();

            if (labelName == "Two Pairs")
            {
                if (roundNumsDistinct == 1)
                {
                    return new Tuple<List<int>, double>(roundNumsDistinctList, (((double)(20) / (double)(40)) * 100));
                }
                else if (roundNumsDistinct == 2)
                {
                    if (numbersChances.Any(x => x.Item2 == 40))
                    {
                        return new Tuple<List<int>, double>(roundNumsDistinctList, 100);
                    }
                    else
                    {
                        return new Tuple<List<int>, double>(roundNumsDistinctList, (((double)(30) / (double)(40)) * 100));
                    }
                }
                else if (roundNumsDistinct == 3)
                {
                    if (numbersChances.Any(x => x.Item2 >= 40))
                    {
                        return new Tuple<List<int>, double>(pair, 100); ;
                    }
                    else
                    {
                        int maxOne = numbersChances.Where(x => x.Item2 == 20).ToList().Max(x => x.Item1);
                        List<int> maxone = new List<int>() { maxOne };
                        List<int> nums = maxone.Union(pair).ToList();
                        return new Tuple<List<int>, double>(nums, (((double)(30) / (double)(40)) * 100));
                    }
                }
                else if (roundNumsDistinct == 4)
                {
                    int maxOne = numbersChances.Where(x => x.Item2 == 20).ToList().Max(x => x.Item1);
                    List<int> maxone = new List<int>() { maxOne };
                    List<int> nums = maxone.Union(pair).ToList();
                    return new Tuple<List<int>, double>(nums, (((double)(30) / (double)(40)) * 100));
                }
                else
                {
                    List<int> maxes = numbersChances.Select(x => x.Item1).ToList().OrderByDescending(x => x).Take(2).ToList();
                    return new Tuple<List<int>, double>(maxes, (((double)(20) / (double)(40)) * 100));
                }
            }
            else
            {
                if (roundNumsDistinct == 1)
                {
                    return new Tuple<List<int>, double>(roundNumsDistinctList, (((double)(30) / (double)(50)) * 100));
                }
                else if (roundNumsDistinct == 2)
                {
                    if (numbersChances.Any(x => x.Item2 == 40))
                    {
                        return new Tuple<List<int>, double>(roundNumsDistinctList, 100);
                    }
                    else
                    {
                        return new Tuple<List<int>, double>(roundNumsDistinctList, (((double)(40) / (double)(50)) * 100));
                    }
                }
                else if (roundNumsDistinct == 3)
                {
                    if (numbersChances.Any(x => x.Item2 >= 40))
                    {
                        return new Tuple<List<int>, double>(pair, (((double)(40) / (double)(50)) * 100)); ;
                    }
                    else
                    {
                        int maxOne = numbersChances.Where(x => x.Item2 == 20).ToList().Max(x => x.Item1);
                        List<int> maxone = new List<int>() { maxOne };
                        List<int> nums = maxone.Union(pair).ToList();
                        return new Tuple<List<int>, double>(nums, (((double)(40) / (double)(50)) * 100));
                    }
                }
                else if (roundNumsDistinct == 4)
                {
                    int maxOne = numbersChances.Where(x => x.Item2 == 20).ToList().Max(x => x.Item1);
                    List<int> maxone = new List<int>() { maxOne };
                    List<int> nums = maxone.Union(pair).ToList();
                    return new Tuple<List<int>, double>(nums, (((double)(30) / (double)(50)) * 100));
                }
                else
                {
                    List<int> maxes = numbersChances.Select(x => x.Item1).ToList().OrderByDescending(x => x).Take(2).ToList();
                    return new Tuple<List<int>, double>(maxes, (((double)(20) / (double)(50)) * 100));
                }
            }
        }

        bool CheckIfLabelIsEmpty(Label label)
        {
            return label.BackColor == Color.White && label.Text == "";
        }

        void FillMachineLabelWithPoint(Label choiceLabel, int point)
        {
            choiceLabel.Text = point.ToString();
            choiceLabel.BackColor = Color.Lime;
            machineTotal += Convert.ToInt32(choiceLabel.Text);
            machine_Total.Text = machineTotal.ToString();
        }

        string ChangeLabelName(Label choiceLabel)
        {
            List<string> labelName = choiceLabel.Name.Split('_').ToList();
            if (labelName.Count == 2)
            {
                return labelName[1];
            }
            else
            {
                return labelName[1] + " " + labelName[2];
            }
        }

        private void RollButton_Click(object sender, EventArgs e)
        {
            userRoundCount++;
            if (userRoundCount <= 3)
            {
                foreach (PictureBox pb in dices)
                {
                    if (!dicesSelectedSources.Contains(pb.ImageLocation))
                    {
                        RollingDices(pb);
                    }
                }
            }
            else
            {
                MessageBox.Show("No more rolls, select your final dices and end the round!");
            }
        }

        private void EndButton_Click(object sender, EventArgs e)
        {
            userRoundCount = 3;
            Play();
        }

        private void DiceTwo_DoubleClick(object sender, EventArgs e)
        {
            PictureBox dice = (PictureBox)sender;
            if (dice.ImageLocation.Split('.').Last() == "jpg")
            {
                ChangeDiceColor(dice, dicesSelectedSources);
            }
            else
            {
                ChangeDiceColor(dice, dicesSources);
            }
        }

        void ChangeDiceColor(PictureBox dice, List<string> list)
        {
            char diceName = dice.ImageLocation.Split('/').Last().Split('.').First().Last();

            foreach (string s in list)
            {
                char name = s.Split('/').Last().Split('.').First().Last();
                if (name == diceName)
                {
                    dice.Image = Image.FromFile(s);
                    dice.ImageLocation = s;
                    dice.Name = diceName.ToString();
                    if(list == dicesSelectedSources)
                    { 
                        dicesHeldDown.Add(dice);
                    }
                    else
                    {
                        dicesHeldDown.Remove(dice);
                    }
                }
            }
        }

        void ShowFinalPoints()
        {
            dicesToSaveLabel.Visible = true;
            roundResultLabel.Visible = true;
            finalNumbers.Clear();
            if (dicesHeldDownLabel.Text == "")
            {
                foreach (PictureBox pb in dicesHeldDown)
                {
                    dicesHeldDownLabel.Text += pb.Name + " ";
                    int num = Convert.ToInt32(pb.Name);
                    finalNumbers.Add(num);
                }

                if (finalNumbers.Count > 0)
                {
                    CalculateResults(finalNumbers);
                    FillResultsLabels(roundResultsLabel.Text);
                }
                else
                {
                    MessageBox.Show("Select your numbers for ending the round!");
                }
            }
        }

        void CalculateNumberResults(List<int> listDicesNums)
        {
            List<int> listDicesNumsDistinct = listDicesNums.Distinct().ToList();

            int firstElement = listDicesNumsDistinct[0];

            if (roundResultsLabel.Text == "" && CheckIfNumberLineIsEmpty(firstElement))
            {
                roundResultsLabel.Text += ReturnNumberLine(firstElement);
            }
            else if (roundResultsLabel.Text != "" && CheckIfNumberLineIsEmpty(firstElement))
            {
                roundResultsLabel.Text += " / " + ReturnNumberLine(firstElement);
            }

            if (listDicesNumsDistinct.Count > 1)
            {
                foreach (int i in listDicesNumsDistinct.Where(x => x != firstElement).ToList())
                {
                    if (CheckIfNumberLineIsEmpty(i))
                    {
                        roundResultsLabel.Text += " / " + ReturnNumberLine(i);
                    }
                }
            }
        }

        void CalculateResults(List<int> listDicesNums)
        {
            CalculateNumberResults(listDicesNums);

            if (user_Chance.Text == "")
            {
                if (roundResultsLabel.Text == "")
                {
                    roundResultsLabel.Text += "Chance";
                }
                else
                {
                    roundResultsLabel.Text += " / Chance";
                }
            }
            if (CheckIfLargeStraight(listDicesNums))
            {
                if (user_Large_Straight.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Large Straight";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Large Straight";
                    }
                }
            }
            if (CheckIfSmallStraight(listDicesNums))
            {
                if (user_Small_Straight.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Small Straight";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Small Straight";
                    }
                }
            }
            if (CheckIfFullHouse(listDicesNums))
            {
                if (user_Full_House.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Full House";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Full House";
                    }
                }
            }
            if (CheckIfTwoPairs(listDicesNums))
            {
                if (user_Two_Pairs.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Two Pairs";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Two Pairs";
                    }
                }
            }
            if (CheckIfOnePair(listDicesNums))
            {
                if (user_Pair.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Pair";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Pair";
                    }
                }
            }
            if (CheckIfDrill(listDicesNums))
            {
                if (user_Drill.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Drill";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Drill";
                    }
                }
            }
            if (CheckIfPoker(listDicesNums))
            {
                if (user_Poker.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Poker";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Poker";
                    }
                }
            }
            if (CheckIfYahtzee(listDicesNums))
            {
                if (user_Yahtzee.Text == "")
                {
                    if (roundResultsLabel.Text == "")
                    {
                        roundResultsLabel.Text += "Yahtzee";
                    }
                    else
                    {
                        roundResultsLabel.Text += " / Yahtzee";
                    }
                }
            }
        }

        void FillResultsLabels(string resultsString)
        {
            if (resultsString != "")
            {
                if (resultsString.Contains("/"))
                {
                    List<string> results = resultsString.Split('/').ToList().Select(x => x.Trim()).ToList();

                    foreach (Label l in userLabels.Where(x => x.BackColor != Color.Lime))
                    {
                        List<string> labelNameSplit = l.Name.Split('_').ToList();

                        if (labelNameSplit.Count == 2)
                        {
                            if (results.Contains(l.Name.Split('_')[1]))
                            {
                                l.BackColor = Color.LightBlue;
                                int point = CalculatePoints(l.Name.Split('_')[1], finalNumbers);
                                l.Text = point.ToString();
                            }
                            else
                            {
                                if (l != user_Total)
                                {
                                    l.Text = "0";
                                }
                            }
                        }
                        else
                        {
                            string name = l.Name.Split('_')[1] + " " + l.Name.Split('_')[2];
                            if (results.Contains(name))
                            {
                                l.BackColor = Color.LightBlue;
                                int point = CalculatePoints(name, finalNumbers);
                                l.Text = point.ToString();
                            }
                            else
                            {
                                if (l != user_Total)
                                {
                                    l.Text = "0";
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Label l in userLabels)
                    {
                        List<string> labelNameSplit = l.Name.Split('_').ToList();

                        if (labelNameSplit.Count == 2)
                        {
                            if (resultsString == l.Name.Split('_')[1])
                            {
                                l.BackColor = Color.Lime;
                                int point = CalculatePoints(resultsString, finalNumbers);
                                l.Text = point.ToString();

                                userTotal += point;
                                user_Total.Text = userTotal.ToString();

                                roundEnd = new Timer();
                                roundEnd.Interval = 2000;
                                roundEnd.Enabled = true;
                                roundEnd.Start();
                                roundEnd.Tick += RoundEnd_Tick;
                            }
                        }
                        else
                        {
                            string name = l.Name.Split('_')[1] + " " + l.Name.Split('_')[2];
                            if (resultsString == name)
                            {
                                l.BackColor = Color.Lime;
                                int point = CalculatePoints(resultsString, finalNumbers);
                                l.Text = point.ToString();

                                userTotal += point;
                                user_Total.Text = userTotal.ToString();

                                roundEnd = new Timer();
                                roundEnd.Interval = 2000;
                                roundEnd.Enabled = true;
                                roundEnd.Start();
                                roundEnd.Tick += RoundEnd_Tick;
                            }
                        }
                    }
                }
            }
            else
            {
                List<Label> emptyLabels = userLabels.Where(x => CheckIfLabelIsEmpty(x)).ToList();
                foreach(Label l in emptyLabels)
                {
                    l.Text = "0";
                }
            }
        }

        int CalculatePoints(string choiceName, List<int> numbers)
        {
            List<int> finalNumbersDistinct = numbers.Distinct().OrderByDescending(x => x).ToList();
            int numToReturn = 0;
            switch (choiceName)
            {
                case "Aces": numToReturn = (finalNumbers.Count(x => x == 1)) * 1; break;
                case "Twos": numToReturn = (finalNumbers.Count(x => x == 2)) * 2; break;
                case "Threes": numToReturn = (finalNumbers.Count(x => x == 3)) * 3; break;
                case "Fours": numToReturn = (finalNumbers.Count(x => x == 4)) * 4; break;
                case "Fives": numToReturn = (finalNumbers.Count(x => x == 5)) * 5; break;
                case "Sixes": numToReturn = (finalNumbers.Count(x => x == 6)) * 6; break;
                case "Small Straight": numToReturn = 30; break;
                case "Large Straight": numToReturn = 40; break;
                case "Yahtzee": numToReturn = 50; break;
                case "Full House": numToReturn = 25; break;
                case "Chance": numToReturn = dices.Select(x => Convert.ToInt32(x.Name)).ToList().Sum(); break;
                case "Drill":
                    int count = 0;
                    foreach (int i in finalNumbersDistinct)
                    {
                        if (finalNumbers.Count(x => x == i) >= 3)
                        {
                            count = i * 3;
                        }
                    }
                    numToReturn = count;
                    break;
                case "Two Pairs":
                    List<int> pairs = new List<int>();
                    foreach (int j in finalNumbersDistinct)
                    {
                        if (finalNumbers.Count(x => x == j) >= 2)
                        {
                            pairs.Add(j);
                        }
                    }
                    if (pairs.Count == 2)
                    {
                        numToReturn = (pairs[0] * 2) + (pairs[1] * 2);
                    }
                    else
                    {
                        numToReturn = 0;
                    }
                    break;
                case "Poker":
                    int k = 0;
                    while (k < finalNumbersDistinct.Count && finalNumbers.Count(x => x == finalNumbersDistinct[k]) != 4)
                    {
                        k++;
                    }
                    if (k < finalNumbersDistinct.Count)
                    {
                        numToReturn = finalNumbersDistinct[k] * 4;
                    }
                    break;
                case "Pair":
                    List<int> pair = new List<int>();
                    foreach (int l in finalNumbersDistinct)
                    {
                        if (finalNumbers.Count(x => x == l) >= 2)
                        {
                            pair.Add(l);
                        }
                    }
                    numToReturn = (pair.Max()) * 2;
                    break;
                default: numToReturn = 0; break;
            }
            return numToReturn;
        }

        private void UserLabel_DoubleClick(object sender, EventArgs e)
        {
            Label selectedChoice = (Label)sender;
            selectedChoice.DoubleClick -= UserLabel_DoubleClick;
            userTotal += Convert.ToInt32(selectedChoice.Text);
            user_Total.Text = userTotal.ToString();

            selectedChoice.BackColor = Color.Lime;

            roundEnd = new Timer();
            roundEnd.Interval = 2000;
            roundEnd.Enabled = true;
            roundEnd.Start();
            roundEnd.Tick += RoundEnd_Tick;

            foreach (Label l in userLabels)
            {
                if (l != selectedChoice && l.BackColor != Color.Lime && l != user_Total)
                {
                    l.BackColor = Color.White;
                    l.Text = "";
                }
            }
        }

        void RoundEnd_Tick(object sender, EventArgs e)
        {
            roundEnd.Enabled = false;
            roundEnd.Stop();
            roundEnd.Dispose();

            ClearRound();
            userRoundCount = 0;
            MachineRound();
        }

        void ClearRound()
        {
            dicesToSaveLabel.Visible = false;
            roundResultLabel.Visible = false;

            finalNumbers.Clear();
            dicesHeldDown.Clear();
            dicesHeldDownLabel.Text = "";
            roundResultsLabel.Text = "";

            foreach (PictureBox pb in dices)
            {
                pb.Image = null;
                pb.ImageLocation = null;
            }
        }

        bool CheckIfYahtzee(List<int> listDicesNums)
        {
            return CheckDupsForDupCases(listDicesNums, 5);
        }

        bool CheckIfOnePair(List<int> listDicesNums)
        {
            return CheckDupsForDupCases(listDicesNums, 2);
        }

        bool CheckIfPoker(List<int> listDicesNums)
        {
            return CheckDupsForDupCases(listDicesNums, 4);
        }

        bool CheckIfDrill(List<int> listDicesNums)
        {
            return CheckDupsForDupCases(listDicesNums, 3);
        }

        bool CheckDupsForDupCases(List<int> listDicesNums, int num)
        {
            return listDicesNums.Distinct().ToList().Any(x => listDicesNums.Count(y => y == x) >= num);
        }

        bool CheckIfTwoPairs(List<int> listDicesNums)
        {
            List<int> pairs = new List<int>();
            foreach (int i in listDicesNums.Distinct().ToList())
            {
                int countInNums = listDicesNums.Count(x => x == i);
                if (countInNums >= 2)
                {
                    pairs.Add(i);
                }
            }

            if (pairs.Count == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool CheckIfFullHouse(List<int> listDicesNums)
        {
            List<int> listDicesNumsDistinct = listDicesNums.Distinct().ToList();

            if (listDicesNumsDistinct.Count == 2 && listDicesNums.Count == 5)
            {
                return listDicesNums.Count(x => x == listDicesNumsDistinct[0]) == 2
                       || listDicesNums.Count(x => x == listDicesNumsDistinct[0]) == 3;
            }
            return false;
        }

        bool CheckIfSmallStraight(List<int> listDicesNums)
        {
            List<int> smallStraight1 = new List<int>() { 1, 2, 3, 4 };
            List<int> smallStraight2 = new List<int>() { 2, 3, 4, 5 };
            List<int> smallStraight3 = new List<int>() { 3, 4, 5, 6 };

            bool list1WithDuplicte = CheckDupsExtrasForSmallStraight(listDicesNums, smallStraight1, -1);
            bool list2WithDuplicte = CheckDupsExtrasForSmallStraight(listDicesNums, smallStraight2, -1);
            bool list3WithDuplicte = CheckDupsExtrasForSmallStraight(listDicesNums, smallStraight3, -1);

            bool list1WithExtraNum = CheckDupsExtrasForSmallStraight(listDicesNums, smallStraight1, 6);
            bool list3WithExtraNum = CheckDupsExtrasForSmallStraight(listDicesNums, smallStraight3, 1);

            if (listDicesNums.Distinct().Count() == 4)
            {
                if (list1WithDuplicte == true || list2WithDuplicte == true || list3WithDuplicte == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (listDicesNums.Distinct().Count() == 5)
            {
                if (CheckIfLargeStraight(listDicesNums)
                   || list1WithDuplicte == true || list2WithDuplicte == true || list3WithDuplicte == true
                   || list1WithExtraNum || list3WithExtraNum)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        bool CheckDupsExtrasForSmallStraight(List<int> dicesNums, List<int> smallStraight, int num)
        {
            if(num == -1)
            {
                return dicesNums.Distinct().OrderBy(x => x).SequenceEqual(smallStraight.OrderBy(x => x));
            }
            else
            {
                return dicesNums.Except(smallStraight).ToList().Count() == 1 && dicesNums.Except(smallStraight).ToList()[0] == num;
            }
        }

        bool CheckIfLargeStraight(List<int> listDicesNums)
        {
            List<int> largeStraight1 = new List<int>() { 1, 2, 3, 4, 5 };
            List<int> largeStraight2 = new List<int>() { 2, 3, 4, 5, 6 };

            if (largeStraight1.Except(listDicesNums).Count() == 0
               || largeStraight2.Except(listDicesNums).Count() == 0)
            {
                return true;
            }
            return false;
        }

        bool CheckIfNumberLineIsEmpty(int num)
        {
            if (ReturnNumberLine(num) != "")
            {
                string name = "user_" + ReturnNumberLine(num);
                return Controls.OfType<Label>().FirstOrDefault(x => x.Name == name).Text == "";

            }
            else
            {
                return false;
            }
        }

        string ReturnNumberLine(int num)
        {
            string numToReturn;
            switch (num)
            {
                case 1: numToReturn = "Aces"; break;
                case 2: numToReturn = "Twos"; break;
                case 3: numToReturn = "Threes"; break;
                case 4: numToReturn = "Fours"; break;
                case 5: numToReturn = "Fives"; break;
                case 6: numToReturn = "Sixes"; break;
                default: numToReturn = ""; break;
            }
            return numToReturn;
        }

        int ReturnNumber(string num)
        {
            int numToReturn;
            switch (num)
            {
                case "Aces": numToReturn = 1; break;
                case "Twos": numToReturn = 2; break;
                case "Threes": numToReturn = 3; break;
                case "Fours": numToReturn = 4; break;
                case "Fives": numToReturn = 5; break;
                case "Sixes": numToReturn = 6; break;
                default: numToReturn = 0; break;
            }
            return numToReturn;
        }

        private void PlayAgainButton_Click(object sender, EventArgs e)
        {
            ClearRound();
            userRoundCount = 0;
            machineRoundCount = 0;
            userTotal = 0;
            machineTotal = 0;

            List<Label> allLabels = machineLabels.Concat(userLabels).ToList();
            foreach(Label l in allLabels)
            {
                l.Text = "";
                l.BackColor = Color.White;
            }
            roundResultsLabel.Text = "";
            Play();
        }
    }
}