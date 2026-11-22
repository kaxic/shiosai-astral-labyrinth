using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AstralGame : StudioGame
{
    public static string[] JobNames => AstralJourney.Jobs;
    public static string[] JobNotes => AstralJourney.JobLore;
    public static string[] SkillNames => AstralJourney.Skills;
    public static string[] SkillNotes => AstralJourney.SkillLore;
    public static string[] ChallengeNames => AstralJourney.Challenges;
    public static string[] ChallengeNotes => AstralJourney.ChallengeLore;
    public static string[] FoeNames => AstralJourney.Foes;
    public static string[] FoeNotes => AstralJourney.FoeLore;
    AstralJourney voyage;
    AstralView view;
    AstralHud hud;
    float flashAt = -100, nextRepeat;
    int epoch;
    Vector2Int held;
    AstralRecord S => voyage.S;
    public AstralMode Mode => S.mode;
    public int Cols => AstralChart.Width;
    public int Rows => AstralChart.Height;
    public int Floor => S.floor;
    public int MaxFloor => AstralChart.Summit;
    public int Job => S.job;
    public int Turn => S.turn;
    public int Seed => S.seed;
    public bool IsDaily => S.dailyDate > 0;
    public string RouteLabel => IsDaily ? $"日替わり {S.dailyDate / 10000}.{S.dailyDate / 100 % 100:00}.{S.dailyDate % 100:00}" : "自由航路";
    public int Hp => S.hp;
    public int MaxHp => voyage.MaxHp;
    public int Focus => S.focus;
    public int MaxFocus => voyage.MaxFocus;
    public int Potions => S.potions;
    public int AttackPower => voyage.Power;
    public int SkillWait => S.skillWait;
    public bool CanLight => S.focus > 0 || S.freeLight > 0;
    public int Lit => S.lit;
    public int BestFloor => S.bestFloor;
    public int Wins => S.wins;
    public int Runs => S.runs;
    public int Kills => S.kills;
    public int BestChallenge => S.bestChallenge;
    public int Challenge => S.challenge;
    public int Beacons => S.beacons;
    public int RelicCount => S.relics.Count;
    public bool ExitReady => voyage.ExitReady;
    public bool Won => S.won;
    public Vector2Int Hero => S.hero;
    public Vector2Int Facing => S.facing;
    public List<AstralFoe> Enemies => S.enemies;
    public int StepSerial => S.turn;
    public int MapVersion => epoch + voyage.Version;
    public int Selection { get; private set; }
    public float Flash => Mathf.Clamp01(1 - (Time.unscaledTime - flashAt) * 4);
    public string LastAction => S.lastAction;
    public string LastLog => S.log;
    public string FloorName => Floor <= 2 ? "天文廃苑" : Floor <= 4 ? "鏡水宮" : Floor <= 6 ? "緋晶工房" : "星冠中枢";
    public string Loadout => S.relics.Count == 0 ? "遺物なし" : string.Join("・", S.relics.ConvertAll(i => AstralJourney.Charms[i]));
    public string Objective => Beacons == 0 ? "二つの星の碑のうち一基を灯そう" :
        !ExitReady ? "星冠の番人を退けて階段を開こう" : Floor == MaxFloor ? "階段に立ち、Eで星冠を灯そう" : "戦術図で階段を確かめ、Eで次の星環へ";
    public string ResultText => Won ? (Challenge > 0 ? ChallengeNames[Challenge] + "を踏破。" : "七つの星環をつなぎ、変則航路が開いた。") +
        $" {Turn}手で星図を完成した。" : $"第{Floor}星環で灯が消えた。最高到達階と観測した魔物は航行録に残る。";
    public string Prompt
    {
        get
        {
            if (Mode != AstralMode.Explore) return "";
            switch (Cell(Hero.x, Hero.y))
            {
                case AstralTile.Beacon: return "E　星の碑を灯す";
                case AstralTile.Chest: return "E　古星箱を開ける";
                case AstralTile.Shrine: return "E　祭壇で遺物を選ぶ";
                case AstralTile.Stairs: return ExitReady ? (Floor == MaxFloor ? "E　星冠を灯す" : "E　次の星環へ") : "階段の封印　" + (Beacons == 0 ? "星の碑が必要" : "番人を倒す");
                default: return "";
            }
        }
    }
    public int SightCount
    {
        get { int count = 0; for (int i = 0; i < FoeNames.Length; i++) if (HasSeenFoe(i)) count++; return count; }
    }
    public bool HasSeenFoe(int i) => i >= 0 && i < FoeNames.Length && (S.foeBits & (1 << i)) != 0;
    public string RelicName(int i) => AstralJourney.Charms[S.offers[i]];
    public string RelicText(int i) => AstralJourney.CharmLore[S.offers[i]];
    bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Cols && y < Rows;
    public bool Seen(int x, int y) => Inside(x,y) && S.seen[y * Cols + x];
    public bool Visible(int x,int y) => Inside(x,y) && S.visible[y * Cols + x];
    public AstralTile Cell(int x, int y) => voyage.Cell(x, y);
    public bool TargetsHero(AstralFoe foe) => foe != null && voyage.Threatens(foe, Hero);
    public bool ThreatAt(int x, int y)
    {
        var p = new Vector2Int(x,y);
        foreach (var foe in S.enemies)
            if (Visible(foe.pos.x, foe.pos.y) && voyage.Threatens(foe, p)) return true;
        return false;
    }

    protected override void BeginGame()
    {
        voyage = new AstralJourney(Load<AstralRecord>()); epoch += 100000;
        hud = new AstralHud(); view = GetComponent<AstralView>();
        if (!view) view = gameObject.AddComponent<AstralView>();
        view.Render(this);
    }
    protected override void LateGame()
    {
        if (!view || voyage == null) return;
        SetIntenseMusic(Playing && Mode == AstralMode.Explore && S.enemies.Exists(e =>
            (e.kind == 4 || e.intent > 0) && Visible(e.pos.x,e.pos.y)));
        view.Render(this);
    }
    protected override void DrawGame() { hud.Draw(this); }
    protected override void SaveCheckpoint() { if (voyage != null) Save(S); }
    public void ChooseJob(int i) { if (voyage.ChooseJob(i)) { Selection = i; Tone(1); } }
    public void StartRun()
    {
        if (Mode != AstralMode.Briefing) return;
        int seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
        voyage.Launch(seed); Selection = 0; Tone(3); SaveCheckpoint();
    }
    public void ReplaySeed()
    {
        if (Mode != AstralMode.Result) return;
        voyage.Launch(S.seed, S.dailyDate); Selection = 0; Tone(3); SaveCheckpoint();
    }
    public void StartDaily()
    {
        if (Mode != AstralMode.Briefing) return;
        int day = AstralJourney.DailySeed(DateTime.Now);
        voyage.Launch(day, day); Selection = 0; Tone(3); SaveCheckpoint();
    }
    public void CycleChallenge()
    {
        if (Wins == 0) { Notice("七つの星環を踏破すると変則航路が開きます"); return; }
        voyage.NextChallenge(); Tone(1); SaveCheckpoint();
    }
    public void NewVoyage() { voyage.Briefing(); Selection = Job; SaveCheckpoint(); }
    public void OpenMap() { voyage.ToggleMap(); held = Vector2Int.zero; }
    public void OpenRecords() { voyage.ToggleRecords(); held = Vector2Int.zero; }
    public void Back() { if (Mode == AstralMode.Map) OpenMap(); else if (Mode == AstralMode.Records) OpenRecords(); }
    public void Move(int dx, int dy) { Complete(voyage.Move(new Vector2Int(dx,dy))); }
    public void WaitTurn() { Complete(voyage.Wait()); }
    public void UseSkill()
    {
        bool ok = voyage.Skill(); Complete(ok);
        if (!ok) Notice(SkillWait > 0 ? $"職技はあと{SkillWait}手" : Focus == 0 ? "焦点が足りない" : "技の届く相手がいない");
    }
    public void UseLight() { bool ok = voyage.Light(); Complete(ok); if (!ok) Notice("焦点が足りない"); }
    public void UsePotion() { bool ok = voyage.Drink(); Complete(ok); if (!ok) Notice(Potions == 0 ? "薬を持っていない" : "体力は全快"); }
    public void Interact() { bool ok = voyage.Interact(); Complete(ok); if (!ok) Notice(Prompt.Length == 0 ? "調べられる物のマスへ移動しよう" : Objective); }
    public void SelectRelic(int i) { if (voyage.ChooseRelic(i)) { Selection = 0; Tone(3); SaveCheckpoint(); } }
    void Complete(bool ok)
    {
        if (!ok) return;
        flashAt = Time.unscaledTime;
        if (Mode == AstralMode.Relic || Mode == AstralMode.Result) { Selection = 0; held = Vector2Int.zero; }
        int sound = LastAction switch { "hit" => 0, "light" => 3, "skill" => 3, "heal" => 3, "open" => 1, _ => -1 };
        if (Mode == AstralMode.Result) sound = Won ? 4 : 2;
        if (sound >= 0) Tone(sound);
        SaveCheckpoint();
    }
    protected override bool HandleEscape()
    {
        if (Mode != AstralMode.Map && Mode != AstralMode.Records) return false;
        Back(); return true;
    }
    static Vector2Int Direction(bool press)
    {
        bool Key(KeyCode key) => press ? Input.GetKeyDown(key) : Input.GetKey(key);
        if (Key(KeyCode.UpArrow) || Key(KeyCode.W)) return Vector2Int.up;
        if (Key(KeyCode.DownArrow) || Key(KeyCode.S)) return Vector2Int.down;
        if (Key(KeyCode.LeftArrow) || Key(KeyCode.A)) return Vector2Int.left;
        if (Key(KeyCode.RightArrow) || Key(KeyCode.D)) return Vector2Int.right;
        return Vector2Int.zero;
    }
    protected override void TickGame()
    {
        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        Vector2Int dir = Direction(true);
        if (Input.GetKeyDown(KeyCode.B)) { OpenRecords(); return; }
        if (Mode == AstralMode.Map || Mode == AstralMode.Records)
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Backspace)) Back();
            else if (Mode == AstralMode.Map && Input.GetKeyDown(KeyCode.E)) hud.ToggleChartSection();
            else if (dir != Vector2Int.zero) hud.Browse(dir.x,dir.y);
            return;
        }
        if (Mode == AstralMode.Briefing)
        {
            if (Input.GetKeyDown(KeyCode.D)) { StartDaily(); return; }
            if (Input.GetKeyDown(KeyCode.C)) CycleChallenge();
            if (dir != Vector2Int.zero) ChooseJob((Job + dir.x - dir.y + 3) % 3);
            if (confirm) StartRun();
            return;
        }
        if (Mode == AstralMode.Relic || Mode == AstralMode.Result)
        {
            if (dir != Vector2Int.zero) { Selection = (Selection + dir.x - dir.y + 3) % 3; Tone(1); }
            if (!confirm) return;
            if (Mode == AstralMode.Relic) SelectRelic(Selection);
            else if (Selection == 0) NewVoyage(); else if (Selection == 1) ReplaySeed(); else OpenRecords();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Tab)) { OpenMap(); return; }
        if (Input.GetKeyDown(KeyCode.E)) { Interact(); return; }
        if (Input.GetKeyDown(KeyCode.F)) { UseLight(); return; }
        if (Input.GetKeyDown(KeyCode.J)) { UseSkill(); return; }
        if (Input.GetKeyDown(KeyCode.Q)) { UsePotion(); return; }
        if (Input.GetKeyDown(KeyCode.Space)) { WaitTurn(); return; }
        if (dir != Vector2Int.zero)
        {
            held = dir; nextRepeat = Time.unscaledTime + .36f; Move(dir.x, dir.y); return;
        }
        var down = Direction(false);
        if (down == Vector2Int.zero || down != held) { held = Vector2Int.zero; return; }
        // Held keys speed up empty corridors. Encountering a visible foe returns control to single keypresses.
        if (Time.unscaledTime >= nextRepeat && !S.enemies.Exists(e => Visible(e.pos.x, e.pos.y)))
        { nextRepeat = Time.unscaledTime + .17f; Move(down.x, down.y); }
    }
    public override void VerifyGame()
    {
        var record = AstralVerification.Run();
        if (!Reviewing) return;
        Save(record);
        var restored = new AstralJourney(Load<AstralRecord>());
        if (restored.S.wins != record.wins || restored.S.bestChallenge != record.bestChallenge || restored.S.foeBits != record.foeBits)
            throw new InvalidOperationException("航行録のディスク再開に失敗しました");
    }
    public override int ShowcaseViews => 3;
    public override void PrepareShowcase() => ShowcaseView(0);
    public override void ShowcaseView(int index)
    {
        epoch += 100000;
        if (AstralVerification.Showcase != null) voyage = new AstralJourney(JsonUtility.FromJson<AstralRecord>(AstralVerification.Showcase));
        else { voyage = new AstralJourney(new AstralRecord()); voyage.Launch(318842); }
        if (index == 1) { S.mode = AstralMode.Briefing; S.job = 1; Selection = 1; }
        else if (index == 2) S.mode = AstralMode.Map;
        else S.mode = AstralMode.Explore;
        view.Render(this);
    }
}
