using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AstralJourney
{
    public static readonly string[] Jobs = { "星盾衛", "星詠師", "風渡り" };
    public static readonly string[] JobLore = { "厚い盾と長剣で接近戦を耐える。体力38、攻撃5、焦点4。", "星を読み、複数の敵の手を止める。体力29、攻撃4、焦点6。", "広い視界と遠矢で先手を取る。体力33、攻撃4、焦点5。" };
    public static readonly string[] Skills = { "星盾の輪", "星線封印", "彗星の遠矢" };
    public static readonly string[] SkillLore = { "周囲2マスの敵を斬り、2手ぶん被害を軽減。", "見えている敵の予兆を消し、2手行動を封じる。", "見えている近敵を6マスまで射抜く。予兆も解除。" };
    public static readonly string[] Challenges = { "通常航路", "薄明航路", "鏡衛航路", "逆星航路" };
    public static readonly string[] ChallengeLore = { "七層の星環を巡る標準の航路。", "自然な視界が1マス短い。各階の最初の投光は無料。", "鏡眼の増援が出現し、魔物の攻撃が1強くなる。", "閉じた扉と隠し罠が増える。碑で焦点を2回復。" };
    public static readonly string[] Foes = { "星虫", "鏡眼", "影狼", "封杖の司祭", "星冠の番人" };
    public static readonly string[] FoeLore = { "近づいて一地点を狙う。赤い印を外せば空振りになる。", "縦横に並ぶ相手へ直線の予兆。射線の横へ出よう。", "数歩先の地点に跳びかかる。着地点をずらすと隙ができる。", "2手後、狙った地点とその上下左右に封印波を放つ。", "広域の星波と直線・一点の攻撃を交互に使う。第四・七層の出口を守る。" };
    public static readonly string[] Charms = { "彗星の刃片", "常夜の果実", "遠見の方位針", "眠り猫の鈴", "銀燐の鎖帷子", "満ち潮の小瓶", "双星の水晶", "宵砂の灯皿", "巡礼の羽" };
    public static readonly string[] CharmLore = { "通常攻撃と攻撃系の職技の威力 +2。", "最大体力 +8、今の体力も8回復。", "自然な視界が1マス広がる。", "封印中・予兆中の敵への通常攻撃 +3。", "魔物から受けるダメージを1軽減。", "薬の回復量 +7、薬を1個補充。", "最大焦点 +2、焦点を2回復。", "各階の最初の投光に焦点を消費しない。", "新しい階に到着すると体力をさらに6回復。" };
    static readonly int[] BaseHp = {38, 29, 33}, BasePower = {5, 4, 4}, BaseFocus = {4, 6, 5};
    public AstralRecord S { get; private set; }
    public int Version { get; private set; }
    public int MaxHp => BaseHp[S.job] + 8 * Has(1) + S.floor - 1;
    public int Power => BasePower[S.job] + 2 * Has(0);
    public int MaxFocus => BaseFocus[S.job] + 2 * Has(6);
    public bool ExitReady => S.beacons > 0 && !S.enemies.Exists(e => e.kind == 4);
    int Has(int id) => S.relics.Contains(id) ? 1 : 0;

    public AstralJourney(AstralRecord record)
    {
        S = record ?? new AstralRecord();
        S.job = Mathf.Clamp(S.job, 0, 2); S.floor = Mathf.Clamp(S.floor, 1, AstralChart.Summit);
        S.challenge = Mathf.Clamp(S.challenge, 0, 3);
        S.enemies ??= new List<AstralFoe>(); S.relics ??= new List<int>();
        S.relics.RemoveAll(i => i < 0 || i >= Charms.Length);
        if (S.offers == null || S.offers.Length != 3) S.offers = new[] {0, 1, 2};
        for (int i = 0; i < 3; i++) S.offers[i] = Mathf.Clamp(S.offers[i], 0, Charms.Length - 1);
        if (!Enum.IsDefined(typeof(AstralMode), S.mode)) S.mode = AstralMode.Briefing;
        if (S.tiles == null || S.tiles.Length != AstralChart.Size || !AstralChart.Inside(S.hero))
        {
            S.mode = AstralMode.Briefing; S.floor = 1;
            AstralChart.Generate(S); S.hp = MaxHp; S.focus = MaxFocus;
        }
        if (S.seen?.Length != AstralChart.Size) S.seen = new bool[AstralChart.Size];
        if (S.visible?.Length != AstralChart.Size) S.visible = new bool[AstralChart.Size];
        if (S.visited?.Length != AstralChart.Size) S.visited = new bool[AstralChart.Size];
        if (S.foundTraps?.Length != AstralChart.Size) S.foundTraps = new bool[AstralChart.Size];
        S.enemies.RemoveAll(e => e == null || e.kind < 0 || e.kind >= Foes.Length || e.hp <= 0 || !AstralChart.Inside(e.pos));
        RefreshVision(); Version++;
    }
    public static int DailySeed(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;
    public void Launch(int seed, int dailyDate = 0)
    {
        var old = S;
        S = new AstralRecord { bestFloor = old.bestFloor, wins = old.wins, runs = old.runs + 1, totalKills = old.totalKills,
            foeBits = old.foeBits, challengeBits = old.challengeBits, bestChallenge = old.bestChallenge, seed = seed, dailyDate = dailyDate,
            job = old.job, challenge = old.challenge, mode = AstralMode.Explore, potions = 2 };
        AstralChart.Generate(S); S.hp = MaxHp; S.focus = MaxFocus;
        S.freeLight = S.challenge == 1 || Has(7) > 0 ? 1 : 0;
        S.bestFloor = Mathf.Max(1, S.bestFloor);
        S.log = "星の碑をひとつ灯し、階段を探そう。敵の赤い予兆は次の一手で決まる。";
        RefreshVision(); Version++;
    }
    public bool ChooseJob(int i)
    {
        if (S.mode != AstralMode.Briefing || i < 0 || i >= Jobs.Length) return false;
        S.job = i; S.hp = MaxHp; S.focus = MaxFocus; Version++; return true;
    }
    public void NextChallenge()
    {
        if (S.mode != AstralMode.Briefing || S.wins == 0) return;
        S.challenge = (S.challenge + 1) % Challenges.Length; Version++;
    }
    public void Briefing()
    {
        if (S.mode != AstralMode.Result) return;
        S.mode = AstralMode.Briefing; S.floor = 1; S.relics.Clear(); AstralChart.Generate(S);
        S.hp = MaxHp; S.focus = MaxFocus; RefreshVision(); Version++;
    }
    public void ToggleMap()
    {
        if (S.mode == AstralMode.Map) S.mode = AstralMode.Explore;
        else if (S.mode == AstralMode.Explore) S.mode = AstralMode.Map;
        Version++;
    }
    public void ToggleRecords()
    {
        if (S.mode == AstralMode.Records) S.mode = S.beforeRecords;
        else { S.beforeRecords = S.mode; S.mode = AstralMode.Records; }
        Version++;
    }
    public AstralTile Cell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= AstralChart.Width || y >= AstralChart.Height) return AstralTile.Wall;
        int i = y * AstralChart.Width + x;
        return S.tiles[i] == AstralTile.Trap && !S.foundTraps[i] ? AstralTile.Floor : S.tiles[i];
    }
    public AstralFoe At(Vector2Int p) => S.enemies.Find(e => e.pos == p);
    public bool Move(Vector2Int direction)
    {
        if (S.mode != AstralMode.Explore || Math.Abs(direction.x) + Math.Abs(direction.y) != 1) return false;
        var next = S.hero + direction;
        if (!AstralChart.Open(S, next, true)) return false;
        S.facing = direction;
        var enemy = At(next);
        if (enemy != null)
        {
            int damage = Power + ((enemy.frozen > 0 || enemy.intent > 0) ? Has(3) * 3 : 0);
            S.log = Foes[enemy.kind] + "に " + damage + " ダメージ。"; Damage(enemy, damage);
            return EndTurn("hit");
        }
        int i = AstralChart.Id(next);
        if (S.tiles[i] == AstralTile.Door) { S.tiles[i] = AstralTile.OpenDoor; S.log = "扉を開いた。"; return EndTurn("open"); }
        S.hero = next; S.log = "";
        if (!S.visited[i])
        {
            S.visited[i] = true;
            if (++S.exploredSteps % 7 == 0 && S.focus < MaxFocus) { S.focus++; S.log = "新しい星図を描いて焦点 +1。"; }
        }
        switch (S.tiles[i])
        {
            case AstralTile.Potion: S.potions++; S.tiles[i] = AstralTile.Floor; S.log = "星蜜の薬を拾った。"; break;
            case AstralTile.Crystal: S.focus = Mathf.Min(MaxFocus, S.focus + 2); S.tiles[i] = AstralTile.Floor; S.log = "灯晶を拾い焦点を補充。"; break;
            case AstralTile.Trap:
                S.hp -= 3; S.tiles[i] = AstralTile.Floor; S.foundTraps[i] = true; S.log = "星棘の罠。体力 −3。"; break;
        }
        return EndTurn("move");
    }
    public bool Wait()
    {
        if (S.mode != AstralMode.Explore) return false;
        S.log = "足音を聴き、一手待った。"; return EndTurn("wait");
    }
    public bool Drink()
    {
        if (S.mode != AstralMode.Explore || S.potions <= 0 || S.hp >= MaxHp) return false;
        S.potions--; int before = S.hp; S.hp = Mathf.Min(MaxHp, S.hp + 16 + 7 * Has(5));
        S.log = $"星蜜を飲み体力 +{S.hp - before}。"; return EndTurn("heal");
    }
    public bool Light()
    {
        if (S.mode != AstralMode.Explore || (S.focus == 0 && S.freeLight == 0)) return false;
        if (S.freeLight > 0) S.freeLight--; else S.focus--;
        S.lit = 4; int disarmed = 0;
        for (int i = 0; i < S.tiles.Length; i++)
        {
            if (S.tiles[i] != AstralTile.Trap || AstralChart.Distance(S.hero, AstralChart.Point(i)) > 3) continue;
            S.tiles[i] = AstralTile.Floor; S.foundTraps[i] = true; disarmed++;
        }
        S.log = $"投光：壁の向こうを3手照らした。解除した罠 {disarmed}。"; return EndTurn("light");
    }
    public bool Skill()
    {
        if (S.mode != AstralMode.Explore || S.focus == 0 || S.skillWait > 0) return false;
        if (S.job == 0)
        {
            S.guard = 3; S.log = "星盾の輪。近くの敵を薙ぎ、2手身を守る。";
            for (int i = S.enemies.Count - 1; i >= 0; i--)
                if (AstralChart.Distance(S.hero, S.enemies[i].pos) <= 2 && AstralChart.Sight(S, S.hero, S.enemies[i].pos)) Damage(S.enemies[i], Power + 1);
        }
        else if (S.job == 1)
        {
            bool found = false;
            foreach (var foe in S.enemies) if (S.visible[AstralChart.Id(foe.pos)])
            { found = true; foe.intent = 0; foe.frozen = 2; }
            if (!found) return false;
            S.log = "星線封印。見えている敵の攻撃を消し、2手止めた。";
        }
        else
        {
            AstralFoe target = null; int best = 7;
            foreach (var foe in S.enemies)
            {
                int d = AstralChart.Distance(S.hero, foe.pos);
                if (d >= best || !S.visible[AstralChart.Id(foe.pos)] || !AstralChart.Sight(S, S.hero, foe.pos)) continue;
                target = foe; best = d;
            }
            if (target == null) return false;
            target.intent = 0; target.frozen = 1; S.log = "彗星の遠矢で " + Foes[target.kind] + " を射抜いた。";
            Damage(target, Power + 4);
        }
        S.focus--; S.skillWait = 4; return EndTurn("skill");
    }
    public bool Interact()
    {
        if (S.mode != AstralMode.Explore) return false;
        int i = AstralChart.Id(S.hero);
        switch (S.tiles[i])
        {
            case AstralTile.Beacon:
                S.tiles[i] = AstralTile.SpentBeacon; S.beacons++;
                S.focus = Mathf.Min(MaxFocus, S.focus + (S.challenge == 3 ? 2 : 1));
                if (S.beacons > 1) S.potions++;
                // Attunement charts the stair itself, but routes through the fog still need exploring.
                S.seen[AstralChart.Id(S.stairs)] = true;
                S.log = S.beacons == 1 ? "星の碑が階段の位置を示した。" : "二つ目の碑が共鳴し、薬を一つ手に入れた。";
                return EndTurn("open");
            case AstralTile.Chest:
                S.tiles[i] = AstralTile.Floor; S.potions++; S.focus = Mathf.Min(MaxFocus, S.focus + 1);
                S.log = "古星箱から薬と灯晶を入手。"; return EndTurn("open");
            case AstralTile.Shrine:
                S.tiles[i] = AstralTile.Floor; MakeOffers(i); S.log = "祭壇に三つの遺物が浮かぶ。";
                EndTurn("open"); if (S.mode == AstralMode.Explore) S.mode = AstralMode.Relic;
                return true;
            case AstralTile.Stairs:
                if (!ExitReady) return false;
                S.log = "次の星環へ進む。"; EndTurn("open");
                if (S.mode == AstralMode.Result) return true;
                if (S.floor == AstralChart.Summit) Finish(true);
                else
                {
                    S.floor++; S.bestFloor = Mathf.Max(S.bestFloor, S.floor); AstralChart.Generate(S);
                    S.hp = Mathf.Min(MaxHp, S.hp + 9 + 6 * Has(8)); S.focus = Mathf.Min(MaxFocus, S.focus + 2);
                    S.freeLight = S.challenge == 1 || Has(7) > 0 ? 1 : 0;
                    S.log = $"第{S.floor}星環へ。休息で体力と焦点が戻った。";
                    RefreshVision(); Version++;
                }
                return true;
        }
        return false;
    }
    void MakeOffers(int cell)
    {
        var random = new System.Random(unchecked(AstralChart.FloorSeed(S.seed, S.floor, S.challenge) + cell));
        var pool = new List<int>();
        for (int i = 0; i < Charms.Length; i++) if (!S.relics.Contains(i)) pool.Add(i);
        for (int i = 0; i < 3; i++) { int pick = random.Next(pool.Count); S.offers[i] = pool[pick]; pool.RemoveAt(pick); }
    }
    public bool ChooseRelic(int i)
    {
        if (S.mode != AstralMode.Relic || i < 0 || i >= 3) return false;
        int id = S.offers[i]; S.relics.Add(id);
        if (id == 1) S.hp = Mathf.Min(MaxHp, S.hp + 8);
        if (id == 5) S.potions++;
        if (id == 6) S.focus = Mathf.Min(MaxFocus, S.focus + 2);
        if (id == 7) S.freeLight = 1;
        S.mode = AstralMode.Explore; S.log = Charms[id] + "を身につけた。"; RefreshVision(); Version++; return true;
    }
    void Damage(AstralFoe foe, int amount)
    {
        foe.hp -= amount;
        if (foe.hp > 0) return;
        S.enemies.Remove(foe); S.kills++; S.totalKills++;
        if (S.kills % 3 == 0 || foe.kind == 4) S.focus = Mathf.Min(MaxFocus, S.focus + 1);
        S.log += Foes[foe.kind] + "を退けた。";
    }
    bool EndTurn(string action)
    {
        S.lastAction = action; S.turn++; S.enemyPhases++; S.floorTurn++;
        if (S.hp > 0) EnemyTurn();
        S.guard = Mathf.Max(0, S.guard - 1); S.skillWait = Mathf.Max(0, S.skillWait - 1); S.lit = Mathf.Max(0, S.lit - 1);
        if (S.hp <= 0) Finish(false); else RefreshVision();
        if (string.IsNullOrEmpty(S.log)) S.log = "星図を一歩進んだ。";
        Version++; return true;
    }
    void Finish(bool won)
    {
        if (S.mode == AstralMode.Result) return;
        S.hp = Mathf.Max(0, S.hp); S.won = won; S.mode = AstralMode.Result;
        if (won) { S.wins++; if (S.challenge > 0) { S.challengeBits |= 1 << S.challenge; S.bestChallenge = Mathf.Max(S.bestChallenge, S.challenge); } }
        S.log = won ? "星冠が灯り、七つの星環がつながった。" : "灯は消えた。描いた航行録は星図庫に残った。";
        Version++;
    }
    void RefreshVision()
    {
        int radius = (S.job == 2 ? 5 : 4) + Has(2) - (S.challenge == 1 ? 1 : 0);
        for (int i = 0; i < S.visible.Length; i++)
        {
            var p = AstralChart.Point(i); int d = AstralChart.Distance(S.hero, p);
            bool lit = (S.lit > 0 && d <= 7) || (d <= radius && AstralChart.Sight(S, S.hero, p));
            S.visible[i] = lit;
            if (!lit) continue;
            S.seen[i] = true;
            if (d <= 1 || S.lit > 0) S.foundTraps[i] = true;
        }
        if (S.mode == AstralMode.Briefing || S.runs == 0) return;
        foreach (var foe in S.enemies) if (S.visible[AstralChart.Id(foe.pos)]) S.foeBits |= 1 << foe.kind;
    }
    public bool Threatens(AstralFoe foe, Vector2Int p)
    {
        if (foe.frozen > 0 || foe.intent == 0) return false;
        if (foe.intent == 1) return p == foe.aim;
        if (foe.intent == 3) return AstralChart.Distance(p, foe.aim) <= 1;
        if (foe.pos.x == foe.aim.x && p.x == foe.pos.x)
            return (p.y - foe.pos.y) * (p.y - foe.aim.y) <= 0;
        if (foe.pos.y == foe.aim.y && p.y == foe.pos.y)
            return (p.x - foe.pos.x) * (p.x - foe.aim.x) <= 0;
        return false;
    }
    void EnemyTurn()
    {
        int[] field = AstralChart.Distances(S, S.hero, false);
        int taken = 0;
        foreach (var foe in S.enemies)
        {
            if (foe.frozen > 0) { foe.frozen--; continue; }
            if (foe.intent > 0)
            {
                if (--foe.delay > 0) continue;
                if (Threatens(foe, S.hero)) taken += Mathf.Max(1, 3 + S.floor / 3 + (foe.kind == 4 ? 2 : 0) +
                    (S.challenge == 2 ? 1 : 0) - Has(4) - (S.guard > 0 ? 3 : 0));
                if (foe.kind == 2 && AstralChart.Open(S, foe.aim) && foe.aim != S.hero && At(foe.aim) == null) foe.pos = foe.aim;
                foe.intent = 0; continue;
            }
            int distance = AstralChart.Distance(foe.pos, S.hero);
            bool sees = distance <= 6 && AstralChart.Sight(S, foe.pos, S.hero);
            foe.alerted = sees ? 4 : Mathf.Max(0, foe.alerted - 1);
            if (foe.alerted == 0 || field[AstralChart.Id(foe.pos)] < 0) continue;
            bool aligned = S.hero.x == foe.pos.x || S.hero.y == foe.pos.y;
            int intent = 0;
            if (sees && foe.kind == 4 && distance <= 4)
                intent = foe.pattern % 2 == 0 ? 3 : aligned ? 2 : 1;
            else if (sees && foe.kind == 3 && distance <= 4) intent = 3;
            else if (sees && foe.kind == 1 && aligned && distance <= 6) intent = 2;
            else if (sees && foe.kind == 2 && distance <= 3) intent = 1;
            else if (distance == 1) intent = 1;
            if (intent != 0)
            {
                foe.intent = intent; foe.aim = S.hero; foe.delay = intent == 3 ? 2 : 1; foe.pattern++;
                continue;
            }
            int best = field[AstralChart.Id(foe.pos)]; Vector2Int step = foe.pos;
            for (int j = 0; j < 4; j++)
            {
                Vector2Int p = foe.pos + AstralChart.Steps[(j + foe.id) % 4];
                if (!AstralChart.Open(S, p) || p == S.hero || At(p) != null) continue;
                int d = field[AstralChart.Id(p)]; if (d >= 0 && d < best) { best = d; step = p; }
            }
            foe.pos = step;
        }
        if (taken > 0) { S.hp -= taken; S.log += $" 魔物の攻撃で体力 −{taken}。"; }
    }
}
