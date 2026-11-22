using System;
using System.Collections.Generic;
using UnityEngine;

public static class AstralVerification
{
    public static string Showcase { get; private set; }
    static int showScore;

    public static AstralRecord Run()
    {
        VerifyCharts();
        VerifyTactics();
        var run = new AstralJourney(new AstralRecord());
        int totalTurns = 0, totalRelics = 0, totalDrinks = 0;
        Showcase = null; showScore = -1;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (run.S.mode == AstralMode.Result) run.Briefing();
            Check(run.ChooseJob(attempt % 3), "choose profession");
            if (attempt == 3) run.NextChallenge();
            int seed = new[] {18247,61382,77102,20261123}[attempt];
            run.Launch(seed, attempt == 3 ? seed : 0);
            var initial = (AstralTile[])run.S.tiles.Clone();
            int lightUses = 0, skills = 0;
            bool savedFight = false, savedShrine = false;
            for (int guard = 0; guard < 2200 && run.S.mode != AstralMode.Result; guard++)
            {
                if (run.S.mode == AstralMode.Relic)
                {
                    string offers = string.Join(",", run.S.offers);
                    int turn = run.S.turn; run = Restore(run);
                    Check(offers == string.Join(",", run.S.offers), "persisted altar offers"); savedShrine = true;
                    int pick = 0;
                    for (int i=0;i<3;i++) if (run.S.offers[i] == 4 || run.S.offers[i] == 0) pick=i;
                    Check(run.ChooseRelic(pick) && run.S.turn == turn, "one altar interaction");
                    totalRelics++; continue;
                }
                int oldTurn = run.S.turn, oldPhases = run.S.enemyPhases;
                bool done = Pilot(run);
                Check(done && run.S.turn == oldTurn + 1 && run.S.enemyPhases == oldPhases + 1, "one enemy phase per command");
                if (run.S.lastAction == "light") lightUses++;
                if (run.S.lastAction == "skill") skills++;
                if (run.S.lastAction == "heal") totalDrinks++;
                if (!savedFight && run.S.enemies.Exists(e => e.intent > 0))
                {
                    var old = run.S; var enemy = old.enemies.Find(e => e.intent > 0);
                    run = Restore(run);
                    var match = run.S.enemies.Find(e => e.id == enemy.id);
                    Check(match != null && match.intent == enemy.intent && match.aim == enemy.aim && match.delay == enemy.delay &&
                        run.S.hero == old.hero && run.S.turn == old.turn && run.S.focus == old.focus, "reopen live encounter");
                    for (int i=0;i<old.seen.Length;i++) Check(old.seen[i] == run.S.seen[i], "fog memory restored");
                    savedFight = true;
                }
                Capture(run);
            }
            Check(run.S.mode == AstralMode.Result && run.S.won,
                $"seed {seed}, job {run.S.job} should finish (floor {run.S.floor}, HP {run.S.hp}, turn {run.S.turn}, {run.S.log})");
            Check(run.S.bestFloor == AstralChart.Summit && savedFight && savedShrine && lightUses > 0 && skills > 0, "boss, expedition and decisions");
            totalTurns += run.S.turn;
            // The result screen reuses the original seed and ship loadout.
            var replay = Restore(run); replay.Launch(replay.S.seed, replay.S.dailyDate);
            Check(SameMap(initial,replay.S.tiles) && replay.S.job == run.S.job && replay.S.challenge == run.S.challenge, "replay same chart");
        }
        Check(run.S.wins == 4 && run.S.bestChallenge == 1 && run.S.challengeBits == 2 && run.S.foeBits == (1 << AstralJourney.Foes.Length) - 1,
            "three professions, all observed foes and postgame");
        var lost = Restore(run); lost.Briefing(); lost.Launch(132713);
        for (int i = 0; i < 500 && lost.S.mode == AstralMode.Explore; i++)
        {
            var watcher = NearestFoe(lost);
            if (watcher == null || watcher.alerted > 0 || watcher.intent > 0) lost.Wait();
            else Walk(lost, watcher.pos);
        }
        Check(!lost.S.won && lost.S.mode == AstralMode.Result && lost.S.bestFloor == AstralChart.Summit && lost.S.foeBits == (1 << AstralJourney.Foes.Length) - 1 &&
            lost.S.wins == 4, "defeat retains charting record");
        Debug.Log($"Astral verification passed: 960 connected generated floors, all professions and twilight challenge cleared, " +
            $"{totalTurns} committed turns, {totalRelics} altar choices, {totalDrinks} medicines, deterministic replay and expedition recovery.");
        return run.S;
    }

    static void VerifyCharts()
    {
        var shapes = new HashSet<int>();
        for (int seed = 0; seed < 96; seed++) for (int floor = 1; floor <= AstralChart.Summit; floor++)
        {
            var s = new AstralRecord { seed = unchecked(seed * 982451653), floor = floor, challenge = seed % 4 };
            AstralChart.Generate(s);
            var steps = AstralChart.Distances(s,s.hero,true);
            int hash = 17, beacons = 0, shrines = 0;
            for (int i=0;i<s.tiles.Length;i++)
            {
                var t = s.tiles[i];
                if (t != AstralTile.Wall) Check(steps[i] >= 0, "all floor cells reachable");
                if (t == AstralTile.Beacon) beacons++;
                if (t == AstralTile.Shrine) shrines++;
                hash = unchecked(hash * 31 + (t == AstralTile.Wall ? 1 : 0));
            }
            Check(beacons == 2 && shrines == 1 && steps[AstralChart.Id(s.stairs)] > 0, "objective connectivity");
            var spawn = new HashSet<Vector2Int>();
            foreach (var e in s.enemies) Check(spawn.Add(e.pos) && e.pos != s.hero && steps[AstralChart.Id(e.pos)] >= 0, "legal enemy spawn");
            shapes.Add(hash);
        }
        Check(shapes.Count > 620, "distinct room and branch geometry");
        Check(AstralJourney.DailySeed(new DateTime(2026,11,23,1,0,0)) == AstralJourney.DailySeed(new DateTime(2026,11,23,23,59,0)) &&
            AstralJourney.DailySeed(new DateTime(2026,11,23)) != AstralJourney.DailySeed(new DateTime(2026,11,24)), "stable daily chart");
    }

    static void VerifyTactics()
    {
        var run = new AstralJourney(new AstralRecord()); run.ChooseJob(1); run.Launch(31);
        var s = run.S;
        run.ToggleMap(); run.ToggleMap(); run.ToggleRecords(); run.ToggleRecords();
        Check(s.turn == 0 && !run.Move(new Vector2Int(1,1)) && !run.Drink() && !run.Interact(), "inspection and invalid commands cost no turn");
        // A controlled corridor isolates the relative timing of fog, doors, traps and the charged beam.
        for (int i=0;i<s.tiles.Length;i++) s.tiles[i] = AstralTile.Wall;
        for (int x=2;x<=8;x++) s.tiles[AstralChart.Id(new Vector2Int(x,5))] = AstralTile.Floor;
        s.hero = new Vector2Int(3,5); s.tiles[AstralChart.Id(new Vector2Int(4,5))] = AstralTile.Door;
        s.tiles[AstralChart.Id(new Vector2Int(5,5))] = AstralTile.Trap;
        s.enemies.Clear(); s.enemies.Add(new AstralFoe { id=1, kind=1, pos=new Vector2Int(7,5), hp=15,maxHp=15 });
        Array.Clear(s.seen,0,s.seen.Length); run = Restore(run); s = run.S;
        Check(!s.visible[AstralChart.Id(s.enemies[0].pos)], "door blocks ordinary vision");
        int focus = s.focus; var hero = s.hero;
        Check(run.Light() && s.focus == focus-1 && s.visible[AstralChart.Id(s.enemies[0].pos)] &&
            s.tiles[AstralChart.Id(new Vector2Int(5,5))] == AstralTile.Floor, "active reconnaissance and disarm");
        Check(run.Move(Vector2Int.right) && s.hero == hero && s.enemies[0].intent == 2, "door action and aimed beam");
        int hp = s.hp;
        Check(run.Skill() && s.hp == hp && s.enemies[0].intent == 0 && s.enemies[0].frozen == 1, "seal cancels next enemy attack");
        Check(run.Move(Vector2Int.right) && s.hero.x == 4 && s.hp == hp, "second frozen enemy turn");
        run.Wait(); Check(s.enemies[0].intent == 2, "enemy signals again");
        run.Wait(); Check(s.hp < hp && run.Drink() && s.potions == 1, "taking risk and recovery");
        s.focus = 0; int oldTurn = s.turn;
        Check(!run.Light() && s.turn == oldTurn, "unaffordable command is free");
    }

    static bool Pilot(AstralJourney run)
    {
        var s = run.S;
        if (s.hp <= run.MaxHp * .52f && s.potions > 0) return run.Drink();
        if (s.floorTurn == 0 && (s.focus > 0 || s.freeLight > 0)) return run.Light();
        AstralFoe close = null; int range = 100;
        foreach (var foe in s.enemies)
        {
            if (!s.visible[AstralChart.Id(foe.pos)]) continue;
            int distance = AstralChart.Distance(s.hero, foe.pos);
            if (distance < range || (distance == range && foe.hp < close.hp)) { close = foe; range = distance; }
        }
        if (s.focus > 0 && s.skillWait == 0 && close != null &&
            ((s.job == 0 && range <= 2) || (s.job == 1 && s.enemies.Exists(e => e.intent != 0 && s.visible[AstralChart.Id(e.pos)])) ||
             (s.job == 2 && range <= 6)) && run.Skill()) return true;
        if (range == 1) return run.Move(close.pos - s.hero);
        var tile = s.tiles[AstralChart.Id(s.hero)];
        if (tile == AstralTile.Shrine || tile == AstralTile.Chest || tile == AstralTile.Beacon ||
            (tile == AstralTile.Stairs && run.ExitReady)) return run.Interact();
        var distances = AstralChart.Distances(s,s.hero,true);
        Vector2Int goal = s.stairs; int best = int.MaxValue;
        for (int i=0;i<s.tiles.Length;i++)
        {
            int d = distances[i];
            if (d <= 0) continue;
            int priority;
            switch (s.tiles[i])
            {
                case AstralTile.Shrine: priority = 0; break;
                case AstralTile.Beacon: if (s.beacons > 0) continue; priority = 10; break;
                case AstralTile.Potion: case AstralTile.Chest: if (s.potions > 0 || d > 6) continue; priority = -3; break;
                default: continue;
            }
            if (d + priority < best) { best = d + priority; goal = AstralChart.Point(i); }
        }
        if (best == int.MaxValue && !run.ExitReady)
        {
            var keeper = s.enemies.Find(e => e.kind == 4);
            if (keeper != null) goal = keeper.pos;
        }
        return Walk(run, goal);
    }
    static bool Walk(AstralJourney run, Vector2Int target)
    {
        var s = run.S; var field = AstralChart.Distances(s,target,true);
        int best = field[AstralChart.Id(s.hero)]; Vector2Int step = Vector2Int.zero;
        foreach (var dir in AstralChart.Steps)
        {
            var cell = s.hero + dir;
            if (!AstralChart.Open(s,cell,true)) continue;
            int score = field[AstralChart.Id(cell)];
            if (score >= 0 && score < best) { best = score; step = dir; }
        }
        return step == Vector2Int.zero ? run.Wait() : run.Move(step);
    }
    static AstralFoe NearestFoe(AstralJourney run)
    {
        var d = AstralChart.Distances(run.S,run.S.hero,true); AstralFoe result = null; int best = int.MaxValue;
        foreach (var e in run.S.enemies) { int n = d[AstralChart.Id(e.pos)]; if (n >= 0 && n < best) { best=n; result=e; } }
        return result;
    }
    static void Capture(AstralJourney run)
    {
        var s = run.S;
        if (s.floor != 4 || s.mode != AstralMode.Explore) return;
        int score = 0;
        foreach (bool b in s.visible) if (b) score++;
        foreach (var e in s.enemies) if (s.visible[AstralChart.Id(e.pos)]) score += 18 + (e.intent > 0 ? 12 : 0);
        if (score <= showScore) return;
        showScore = score; Showcase = JsonUtility.ToJson(s);
    }
    static bool SameMap(AstralTile[] a,AstralTile[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i=0;i<a.Length;i++) if (a[i] != b[i]) return false;
        return true;
    }
    static AstralJourney Restore(AstralJourney run) => new AstralJourney(JsonUtility.FromJson<AstralRecord>(JsonUtility.ToJson(run.S)));
    static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("Astral voyage check failed: " + label);
    }
}
