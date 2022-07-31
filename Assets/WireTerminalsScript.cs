using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WireTerminalsScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public List<KMSelectable> wires;
    public Renderer[] wrends;
    public Renderer[] leds;
    public Material[] wcols;
    public Material[] lcols;

    private readonly string[] cnames = new string[5] { "Red", "Yellow", "Blue", "White", "Black"};
    private readonly int[][] livecons = new int[10][] { new int[2] { 3, 4}, new int[2] { 1, 2 }, new int[2] { 0, 4}, new int[2] { 1, 3}, new int[2] { 0, 2}, new int[2] { 1, 4}, new int[2] { 2, 3}, new int[2] { 0, 1}, new int[2] { 2, 4}, new int[2] { 0, 3} };
    private int[] lnums = new int[5];
    private int[,] lgrid = new int[4, 4];
    private int[][,] wgrid = new int[3][,] { new int[7,7], new int[7,7], new int[7,7]};
    private string[][] config = new string[15][];
    private bool[][] cut = new bool[2][] { new bool[48], new bool[48] };

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        foreach (Renderer l in leds)
            l.material = lcols[4];
        for(int i = 0; i < 48; i++)
        {
            int r = Random.Range(0, 5);
            int[] p = P(i);
            wgrid[i % 2][p[0], p[1]] = r;
            for (int j = 0; j < 3; j++)
            wrends[3 * i + j].material = wcols[r];
        }
    }

    private void Start()
    {
        lnums[0] = info.GetSerialNumberNumbers().First();
        for(int i = 1; i < 5; i++)
        {
            lnums[i] = (lnums[i - 1] + info.GetSerialNumberNumbers().Skip(1).First()) % 10;
            while (lnums.Take(i).Contains(lnums[i]))
            {
                lnums[i] += 1;
                lnums[i] %= 10;
            }
        }
        Debug.LogFormat("[Wire Terminals #{0}] The terminals transmit through these wires: {1}", moduleID, string.Join(" | ", Enumerable.Range(0, 5).Select(x => cnames[x] + " - " + string.Join(" & ", livecons[lnums[x]].Select(y => cnames[y]).ToArray())).ToArray()));
        foreach (KMSelectable wire in wires)
        {
            int w = wires.IndexOf(wire);
            wire.OnInteract = delegate ()
            {
                if (!moduleSolved)
                {
                    if (cut[0][w])
                    {
                        cut[0][w] = false;
                        cut[1][w] = true;
                        wire.AddInteractionPunch(-0.3f);
                        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
                        wire.gameObject.SetActive(false);
                        int[] p = P(w);
                        wgrid[2][p[0], p[1]] += 1;
                        for (int i = 0; i < 24; i++)
                        {
                            if (cut[1][2 * i] ^ cut[1][(2 * i) + 1])
                            {
                                p = P(2 * i);
                                if (!Connected(p[0], p[1]))
                                {
                                    wgrid[2][p[0], p[1]] = 3;
                                    cut[0][2 * i] = false;
                                    cut[0][(2 * i) + 1] = false;
                                    cut[1][2 * i] = true;
                                    cut[1][(2 * i) + 1] = true;
                                    i = 0;
                                }
                            }
                        }
                        if (cut[1].All(x => x))
                        {
                            moduleSolved = true;
                            module.HandlePass();
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                            for (int i = 0; i < 16; i++)
                                leds[i].material = lcols[5];
                        }
                        else if (cut[0].All(x => !x))
                        {
                            Generate();
                            Debug.LogFormat("[Wire Terminals #{0}] All applicable wires cut. Resetting terminal configuration. Avoid cutting the wires in these positions:", moduleID);
                            Configure();
                        }
                    }
                    else
                    {
                        module.HandleStrike();
                        wire.AddInteractionPunch(2);
                    }
                }
                return false;
            };
        }
        Generate();
        config = new string[15][]
        {
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "RYBWK"[wgrid[0][1, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 6]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 6]].ToString() },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][2, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][2, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][2, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][2, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "RYBWK"[wgrid[0][3, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[1][3, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[0][3, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[1][3, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 6]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 6]].ToString() },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][4, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][4, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][4, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][4, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "RYBWK"[wgrid[0][5, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[1][5, 0]].ToString(), "\u25a1", "RYBWK"[wgrid[0][5, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[1][5, 2]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 4]].ToString(), "\u25a1", "RYBWK"[wgrid[0][1, 6]].ToString(), "\u25a1", "RYBWK"[wgrid[1][1, 6]].ToString() },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][6, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][6, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[0][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0", "\u25a1", "\u25a0" },
            new string[15]{ "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][6, 1]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][6, 3]].ToString(), "\u25a0", "\u25a0", "\u25a0", "RYBWK"[wgrid[1][0, 5]].ToString(), "\u25a0", "\u25a0", "\u25a0" }          
        };
        Debug.LogFormat("[Wire Terminals #{0}] The configuration of wires is:\n[Wire Terminals #{0}] {1}", moduleID, string.Join("\n[Wire Terminals #" + moduleID + "] ", config.Select(x => string.Join("", x)).ToArray()));
        Debug.LogFormat("[Wire Terminals #{0}] The configuration of terminals is as follows, avoid cutting the wires in these positions:", moduleID);
        Configure();
    }

    private int[] P(int i)
    {
        int[] p = new int[2];
        if (i < 24)
        {
            p[0] = 2 * (i / 6);
            p[1] = 2 * ((i / 2) % 3) + 1;
        }
        else
        {
            p[0] = 2 * ((i - 24) / 8) + 1;
            p[1] = 2 * (((i - 24) / 2) % 4);
        }
        return p;
    }

    private void Generate()
    {
        bool success = false;
        while (!success)
        {
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    int r = Random.Range(0, 5);
                    lgrid[i, j] = r;
                    wgrid[0][2 * i, 2 * j] = lnums[r];
                    wgrid[1][2 * i, 2 * j] = lnums[r];
                }
            for(int i = 0; i < 48; i++)
            {
                if (!cut[1][i])
                {
                    int[] p = P(i);
                    int w = wgrid[2][p[0], p[1]];
                    if (w < 2)
                    {
                        w = wgrid[i % 2][p[0], p[1]];
                        if (i < 24)
                        {
                            if (livecons[wgrid[i % 2][p[0], p[1] - 1]].Contains(w) || livecons[wgrid[i % 2][p[0], p[1] + 1]].Contains(w))
                                continue;
                        }
                        else
                        {
                            if (livecons[wgrid[i % 2][p[0] - 1, p[1]]].Contains(w) || livecons[wgrid[i % 2][p[0] + 1, p[1]]].Contains(w))
                                continue;
                        }
                        cut[0][i] = true;
                    }
                }
            }
            success = cut[0].Any(x => x);
        }
        for (int i = 0; i < 16; i++)
            leds[i].material = lcols[lgrid[i / 4, i % 4]];
    }

    private void Configure()
    {
        for (int i = 0; i < 16; i++)
            config[4 * (i / 4) + 1][4 * (i % 4) + 1] = "RYBWK"[lgrid[i / 4, i % 4]].ToString();
        for(int i = 0; i < 12; i++)
        {
            int[] p = P(2 * i);
            config[4 * (i / 3)][4 * (i % 3) + 3] = wgrid[2][p[0], p[1]] < 3 ? (cut[1][2 * i] ? "/" : (cut[0][2 * i] ? "-" : "X")) : "X";
            config[4 * (i / 3) + 2][4 * (i % 3) + 3] = wgrid[2][p[0], p[1]] < 3 ? (cut[1][(2 * i) + 1] ? "/" : (cut[0][(2 * i) + 1] ? "-" : "X")) : "X";
            p = P((2 * i) + 24);
            config[4 * (i / 4) + 3][4 * (i % 4)] = wgrid[2][p[0], p[1]] < 3 ? (cut[1][(2 * i) + 24] ? "/" : (cut[0][(2 * i) + 24] ? "|" : "X")) : "X";
            config[4 * (i / 4) + 3][4 * (i % 4) + 2] = wgrid[2][p[0], p[1]] < 3 ? (cut[1][(2 * i) + 25] ? "/" : (cut[0][(2 * i) + 25] ? "|" : "X")) : "X";
        }
        Debug.LogFormat("[Wire Terminals #{0}] {1}", moduleID, string.Join("\n[Wire Terminals #" + moduleID + "] ", config.Select(x => string.Join("", x)).ToArray()));
    }

    private bool Connected(int j, int k)
    {
        int[][,] tgrid = new int[2][,] { new int[7, 7], new int[7, 7] };
        for (int i = 0; i < 49; i++)
        {
            tgrid[0][i / 7, i % 7] = wgrid[2][i / 7, i % 7];
            tgrid[1][i / 7, i % 7] = wgrid[2][i / 7, i % 7];
        }
        tgrid[0][j, k] = 2;
        tgrid[1][j, k] = 2;
        tgrid[0][2 * Random.Range(0, 4), 2 * Random.Range(0, 4)] = 2;
        while(Enumerable.Range(0, 16).Select(x => tgrid[0][2 * (x / 4), 2 * (x % 4)] == tgrid[1][2 * (x / 4), 2 * (x % 4)]).Any(x => !x))
        {
            for (int i = 0; i < 16; i++)
                tgrid[1][2 * (i / 4), 2 * (i % 4)] = tgrid[0][2 * (i / 4), 2 * (i % 4)];
            for (int i = 0; i < 16; i++)
            {
                int[] p = new int[2] { 2 * (i / 4) , 2 * (i % 4) };
                if(tgrid[0][p[0], p[1]] == 2)
                {
                    tgrid[0][p[0], p[1]] = 3;
                    if (p[0] > 0 && tgrid[0][p[0] - 1, p[1]] != 2 && tgrid[0][p[0] - 2, p[1]] == 0)
                        tgrid[0][p[0] - 2, p[1]] = 1;
                    if (p[1] > 0 && tgrid[0][p[0], p[1] - 1] != 2 && tgrid[0][p[0], p[1] - 2] == 0)
                        tgrid[0][p[0], p[1] - 2] = 1;
                    if (p[0] < 6 && tgrid[0][p[0] + 1, p[1]] != 2 && tgrid[0][p[0] + 2, p[1]] == 0)
                        tgrid[0][p[0] + 2, p[1]] = 1;
                    if (p[1] < 6 && tgrid[0][p[0], p[1] + 1] != 2 && tgrid[0][p[0], p[1] + 2] == 0)
                        tgrid[0][p[0], p[1] + 2] = 1;
                }
            }
            for (int i = 0; i < 16; i++)
            {
                if (tgrid[0][2 * (i / 4), 2 * (i % 4)] == 1)
                    tgrid[0][2 * (i / 4), 2 * (i % 4)] = 2;
            }           
        }
        return Enumerable.Range(0, 16).Select(x => tgrid[0][2 * (x / 4), 2 * (x % 4)] == 3).All(x => x);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cut <1-48> [Cuts wire in specified position in reading order. Chain with spaces.]";
#pragma warning restore 414

    List<int> CutWires = new List<int>();
    int[] ReadingOrderWires = { 1, 3, 5, 2, 4, 6, 25, 26, 27, 28, 29, 30, 31, 32, 7, 9, 11, 8, 10, 12, 33, 34, 35, 36, 37, 38, 39, 40, 13, 15, 17, 14, 16, 18, 41, 42, 43, 44, 45, 46, 47, 48, 19, 21, 23, 20, 22, 24 };

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ').Distinct().ToArray();
        if (Regex.IsMatch(parameters[0], @"^\s*cut\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length < 2)
            {
                yield return "sendtochaterror!f Parameter length invalid.";
                yield break;
            }
            int[] c = new int[parameters.Length - 1];
            int Out;
            for (int i = 0; i < c.Length; i++)
            {
                if (!int.TryParse(parameters[i + 1], out Out) || Out < 1 || Out > 48)
                {
                    yield return "sendtochaterror!f Invalid wire placement number: " + parameters[i + 1];
                    yield break;
                }
                if (CutWires.Contains(Out))
                {
                    yield return "sendtochaterror!f Wire " + parameters[i + 1] + " is already cut.";
                    yield break;
                }
                c[i] = Out;               
            }
            for (int i = 0; i < c.Length; i++)
            {
                wires[ReadingOrderWires[c[i] - 1] - 1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}

