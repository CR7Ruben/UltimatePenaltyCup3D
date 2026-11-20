namespace UltimatePenaltyCup.Core
{
    public enum GameState
    {
        MainMenu,
        RoundSelect,
        Match,
        Paused,
        Result,
        RoundEnd,
        Shop,
        Customization,
        Achievements,
        Settings
    }

    public enum ShotResult
    {
        None,
        GoalNormal,
        GoalCorner,
        GoalHighPower,
        Saved,
        Missed,
        PostHit
    }

    public enum BallType
    {
        Classic,
        Blue,
        Fire,
        Rainbow,
        Gold
    }

    public enum StadiumType
    {
        Cantera,
        Night,
        Professional,
        WorldCup
    }

    public enum RoundId
    {
        Round1_Cantera = 1,
        Round2_LigaLocal = 2,
        Round3_LigaProfesional = 3,
        Round4_Semifinal = 4,
        Round5_GranFinal = 5
    }

    public enum MatchAttemptState
    {
        Aiming,
        Charging,
        Kicking,
        BallInFlight,
        Evaluated,
        Resetting
    }
}
