namespace IndieStudio.Simulation
{
    /// <summary>
    /// Every tunable number of the M1 slice in one place. Values mirror
    /// Design/13_Finance_and_Economy.md and Design/Tools/economy_sim.py — change them together.
    /// </summary>
    public static class Tuning
    {
        // Start (Design/02 §1)
        public const double StartCash = 5000;
        public const double CreditLimit = 4000;
        public const double MonthlyDebtInterest = 0.02; // ≈ 24% APR on the credit card

        // Monthly living costs at start
        public const double RentApartment = 850;
        public const double RentSharedFlat = 550;
        public const double RentParents = 150;
        public const double FoodAndLiving = 450;
        public const double Utilities = 90;
        public const double Internet = 40;
        public const double Phone = 30;
        public const double RentIncreaseMin = 0.03;
        public const double RentIncreaseMax = 0.09;

        // Time
        public const int BlockHours = 4;
        public const double SustainableWeeklyHours = 45;

        // Burnout (Design/02 §5)
        public const double BurnoutPerHourOver = 0.35;
        public const double BurnoutRecoveryPerHourUnder = 0.5;
        /// <summary>Working through the weekend has a social cost. Evenings don't: a 48h week needs them.</summary>
        public const double BurnoutPerWeekendBlock = 0.5;
        public const double BurnoutPerJamBlock = 0.5;
        public const double MoneyAnxietyBurnout = 2;
        public const double MoneyAnxietyRunwayDays = 60;
        public const double BurnoutPenaltyStart = 30;
        public const double BurnoutPenaltyPerPoint = 0.004;
        public const double VacationAdviceBurnout = 60;
        public const double CollapseBurnout = 75;
        public const double CollapseChance = 0.08;
        public const int CollapseDays = 14;

        // Skills (Design/02 §6)
        public const double XpHoursCoefficient = 70;
        public const double LearnXpMultiplier = 1.25;
        public const double CourseXpMultiplier = 1.5;
        public const double JamXpMultiplier = 1.3;

        // Freelance (Design/03 §2)
        public const double RateBase = 5;
        public const double RatePerSkill = 4.5;
        public const double RatingBonusMax = 0.25;
        public const int MaxActiveGigs = 3;

        // Work model (Design/05 §5). Scope bands: Tiny 1–2 pw (≈15 build sessions), Small 20–40 pw (≈300).
        // A Tiny game is a first-month project; a Small game is a real indie game that takes about a year of
        // balanced play, and is out of reach for someone keeping a full-time job (Design/13 §12).
        public const double TinyBasePw = 1.2;
        public const double SmallBasePw = 30;
        public const double PersonWeekHours = 40;
        public const double EfficiencyBase = 0.55;
        public const double EfficiencyPerSkill = 0.09;
        public const double Laptop3DPenalty = 0.8;   // builds and bakes on a cheap laptop
        public const double GamingPc3DFactor = 0.95;
        /// <summary>Roughly how much work one named feature is (Tiny, Small), so a feature takes 2–5 sessions.</summary>
        public const double FeatureSizeTiny = 0.15;
        public const double FeatureSizeSmall = 0.5;

        // Bugs (Design/05 §7): new work is buggy, polishing fixes it. Stability = 10 − 0.25 × bugs left.
        public const double BugsPerPwProgramming = 16;
        public const double BugsPerPwOther = 4;
        public const double BugsFixedPerPolishPw = 60;
        /// <summary>Polish needed for the full polish bonus: 15% of the project's work, at least this much.</summary>
        public const double PolishFloorPw = 0.1;

        // Minigames (Design/02 §4): a score of 0–1 becomes a work multiplier of 0.5–1.2.
        // A score of about 0.7 is an ordinary session (×1.0), which is what bots and the balance tests play.
        public const double MinigameFloor = 0.5;
        public const double MinigameRange = 0.7;
        /// <summary>Task quality moves by this × (multiplier − 1): careful sessions make better features.</summary>
        public const double MinigameQualityBonus = 1.5;
        /// <summary>A minigame score worth three stars (×1.095 or better).</summary>
        public const double ThreeStarScore = 0.85;
        /// <summary>Client score moves by this × (average multiplier − 1).</summary>
        public const double GigScorePerPerformance = 2.5;

        // Flow and small rituals (Design/02 §4)
        /// <summary>Each consecutive session on the same work adds this much output, up to <see cref="FlowMaxBonus"/>.</summary>
        public const double FlowPerSession = 0.05;
        public const double FlowMaxBonus = 0.15;
        /// <summary>Coffee: the next work session ×1.15, for +1 burnout now. At most two a day.</summary>
        public const double CoffeeBoost = 1.15;
        public const double CoffeeBurnout = 1;
        public const int CoffeesPerDay = 2;
        /// <summary>Petting the cat lifts morale once a day, but only when you're low.</summary>
        public const double CatMorale = 2;
        public const double CatMoraleBelow = 60;

        // Estimates: the planning fallacy (Design/05 §6)
        public const double EstimateBiasStart = 0.70;
        public const double EstimateBiasPerProductionLevel = 0.03;
        public const double EstimateBiasPerShippedGame = 0.04;
        public const double EstimateBiasMax = 0.95;

        // Marketing (Design/09 §4; the sim uses ~6 wishlists per marketing hour)
        public const double WishlistsPerMarketingBlock = 24;
        public const double WishlistSigma = 0.8;
        public const double FollowersPerMarketingBlock = 6;
        public const double FollowerSigma = 1.0;
        public const double FollowersWhoWishlistOnPageOpen = 0.25;

        // Store & sales (Design/10, 11, 13)
        public const double VaporStoreFee = 100;
        public const double VaporPlatformFee = 0.30;
        public const double ScratchPlatformFee = 0.10;
        public const double RefundRate = 0.08;
        public const double LaunchDiscount = 0.10;
        public const int LaunchDiscountDays = 7;
        public const int VaporMinStorePageDays = 14;
        public const int VaporPayoutDelayDays = 30;
        public const double WcrBase = 0.10;
        public const double WcrSigma = 0.35;
        public const double OrganicShare = 0.30;
        public const double FollowerBuyRate = 0.03;
        public const double Year1Base = 1.8;
        public const double Year1PerQuality = 2.2;
        public const double MonthlyDecay = 0.78;
        public const double TailSharePerMonth = 0.03;
        public const double ViralQualityGate = 0.6;
        public const double ViralChance = 0.03;
        public const double ViralMultiplierMedian = 8;
        public const double ReviewRate = 0.02;

        // Launch week (Design/10 §7)
        public const int LaunchWeekDays = 7;
        /// <summary>Hotfixes and answers are possible until the post-mortem.</summary>
        public const int PatchWindowDays = 30;
        /// <summary>The chance a hotfix breaks something else: 16%, minus 2.5 points per Programming level, at least 3%.</summary>
        public const double RegressionBase = 0.16;
        public const double RegressionPerSkill = 0.025;
        public const double RegressionMin = 0.03;
        /// <summary>Review points (as a share) won by one session of answering players, and the cap.</summary>
        public const double GoodwillPerAnswer = 0.012;
        public const double GoodwillMax = 0.06;
        /// <summary>Launch week posts reach twice as far, and some of the people they reach buy.</summary>
        public const double LaunchMomentum = 2.0;
        public const double LaunchPushBuyRate = 0.25;
        public const double LaunchPushWishlistRate = 0.004;
        /// <summary>Word of mouth: future sales scale with 0.4 + 1.2 × the review mood.</summary>
        public const double WordOfMouthBase = 0.4;
        public const double WordOfMouthSlope = 1.2;

        // Next Fest and demos (Design/10 §4–5)
        /// <summary>Sign-ups close four weeks before a fest; a fest lasts a week.</summary>
        public const int FestSignUpDays = 28;
        public const int FestDays = 7;
        /// <summary>A decent demo (quality 0.55) of an average-market game brings about this many wishlists, before luck and audience.</summary>
        public const double FestBaseWishlists = 900;
        /// <summary>No demo at all: the page is just in the list.</summary>
        public const double FestNoDemoShare = 0.12;
        public const double FestMarketingBoost = 1.5;
        /// <summary>A demo needs the game this far along; one session adds this much of it (at efficiency 1), and squashes this many of its bugs.</summary>
        public const double DemoMinProgress = 0.30;
        public const double DemoProgressPerSession = 0.18;
        public const double DemoBugsFixedPerSession = 4;
        /// <summary>The demo starts with this share of the game's bugs.</summary>
        public const double DemoBugShare = 0.3;
        public const double DemoPlayersPerWishlist = 3;

        // Taxes
        public const double TaxRate = 0.25;
        public const double TaxAllowance = 10000;

        // Day jobs (Design/03 §3)
        public const double PartTimeMonthlyPay = 1600;
        public const double FullTimeMonthlyPay = 2900;
        /// <summary>Work after a day-job shift on the same day: tired evenings get less done (Design/02 §4).</summary>
        public const double AfterJobFatigue = 0.8;
    }
}
