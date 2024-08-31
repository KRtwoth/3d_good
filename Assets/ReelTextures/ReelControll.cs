using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


public enum ReelState
{
    Stop,
    Spin,
    Slip,
}
public enum SymbolName
{
    NULL = -1,
    GOD = 0,
    ハーデス = 1,
    青七 = 2,
    黄七 = 3,
    赤七 = 4,
}

public enum SlipType
{
    無,
    組優,
    払優
}

public enum PayType
{
    None = 0,
    Pay1 = 1,
    Pay15 = 15,
    ReplayA = 91,
    ReplayB = 92,
    ReplayC = 93,
    ReplayD = 94,
    ReplayE = 95,
    ReplayF = 96,
}

public struct Role
{
    public string name;
    public SymbolName[] symbols;
    public PayType payType;
    public Role(string name, SymbolName[] symbols, PayType payType)
    {
        this.name = name;
        this.symbols = symbols;
        this.payType = payType;
    }
}

public struct ConcurrentRole
{
    public string name;
    public Role[] roles;
    public bool isFree;
    public int[] answerOrder;

    public Role[] freeRolesLeft;
    public Role[] freeRolesCenter;
    public Role[] freeRolesRight;

    public ConcurrentRole(string name, Role[] roles, int[] answerOrder)
    {
        this.isFree = false;
        this.name = name;
        this.roles = roles;
        this.answerOrder = answerOrder;
        freeRolesLeft = new Role[roles.Length];
        freeRolesCenter = new Role[roles.Length];
        freeRolesRight = new Role[roles.Length];
    }

    public ConcurrentRole(string name, Role[] roleLeft, Role[] roleCenter, Role[] roleRight)
    {
        this.name = name;
        this.isFree = true;
        this.roles = new Role[roleLeft.Length];
        this.answerOrder = new int[3] { 0, 1, 2 };
        freeRolesLeft = roleLeft;
        freeRolesCenter = roleCenter;
        freeRolesRight = roleRight;

        freeRolesLeft = new Role[roles.Length];
        freeRolesCenter = new Role[roles.Length];
        freeRolesRight = new Role[roles.Length];
    }
}

public class ReelControll : MonoBehaviour
{
    public RenderTexture[] reelTexture;
    public RenderTexture[] reelEmissonTexture;
    public Texture2D[] symbols;
    public Texture2D reelBackWhite;
    public Texture2D reelBackBlack;
    public GameObject[] reels = new GameObject[3];

    public float MAX_SPIN_SPEED = 8f;
    public static readonly float koma = 20;
    public static float komaAngle = 360 / koma;
    public float[] firstStopAngle = new float[3] { 0, 0, 0 };
    public float[] slipStopAngle = new float[3] { 0, 0, 0 };
    public float[] angle = new float[3] { 0, 0, 0 };
    public int[] answerOrder = new int[3] { 0, 1, 2 };
    public int[] pushOrder = new int[3] { -1, -1, -1 };
    public bool isOrderMiss = false;
    public int pushCount = 0;
    public SlipType[] slipTypes = new SlipType[3];
    public ConcurrentRole roles;
    public SymbolName[] stopedSymbolNames = new SymbolName[3] { SymbolName.NULL, SymbolName.NULL, SymbolName.NULL };

    private float[] rollSpeeds = new float[3] { 0, 0, 0 };
    public ReelState[] reelStates = new ReelState[3];

    private SymbolName[,] symbolIndexes = new SymbolName[3, 20];

    private static Role 中段GOD = new Role("中段GOD", new SymbolName[3] { SymbolName.GOD, SymbolName.GOD, SymbolName.GOD }, PayType.Pay15);

    private static Role クラッシュ = new Role("クラッシュ", new SymbolName[3] { SymbolName.青七, SymbolName.黄七, SymbolName.赤七 }, PayType.ReplayA);

    private static Role 中段ハーデス = new Role("中段ハーデス", new SymbolName[3] { SymbolName.ハーデス, SymbolName.ハーデス, SymbolName.ハーデス }, PayType.ReplayA);
    private static Role 中段ハーデスフェイク = new Role("中段ハーデスフェイク", new SymbolName[3] { SymbolName.GOD, SymbolName.ハーデス, SymbolName.ハーデス }, PayType.ReplayA);

    private static Role 中段赤七 = new Role("中段赤七", new SymbolName[3] { SymbolName.黄七, SymbolName.黄七, SymbolName.黄七 }, PayType.Pay15);
    private static Role 中段赤七フェイク = new Role("中段赤七フェイク", new SymbolName[3] { SymbolName.青七, SymbolName.赤七, SymbolName.赤七 }, PayType.Pay15);

    private static Role 中段黄七 = new Role("中段黄七", new SymbolName[3] { SymbolName.黄七, SymbolName.黄七, SymbolName.黄七 }, PayType.Pay15);

    private static Role 右上がり黄七 = new Role("右上がり黄七", new SymbolName[3] { SymbolName.ハーデス, SymbolName.黄七, SymbolName.青七 }, PayType.ReplayA);

    private static Role 中段青七 = new Role("中段青七", new SymbolName[3] { SymbolName.青七, SymbolName.青七, SymbolName.青七 }, PayType.ReplayA);

    private static Role ハズレ = new Role("ハズレ", new SymbolName[3] { SymbolName.青七, SymbolName.黄七, SymbolName.青七 }, PayType.ReplayA);


    private static ConcurrentRole 中段GOD揃い
        = new ConcurrentRole("中段GOD揃い", new Role[1] { 中段GOD }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole クラッシュ揃い
        = new ConcurrentRole("クラッシュ揃い", new Role[1] { クラッシュ }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole 押し順ハーデス揃い231
        = new ConcurrentRole("押し順ハーデス揃い231", new Role[2] { 中段ハーデス, ハズレ }, new int[3] { 2, 0, 1 });
    private static ConcurrentRole 押し順ハーデスハズレ揃い231
        = new ConcurrentRole("押し順ハーデスハズレ揃い231", new Role[2] { 中段ハーデスフェイク, ハズレ }, new int[3] { 2, 0, 1 });

    private static ConcurrentRole 押し順赤七揃い321
        = new ConcurrentRole("押し順赤七揃い321", new Role[2] { 中段赤七, ハズレ }, new int[3] { 2, 1, 0 });

    private static ConcurrentRole 押し順赤七ハズレ揃い321
        = new ConcurrentRole("押し順赤七ハズレ揃い321", new Role[2] { 中段赤七フェイク, ハズレ }, new int[3] { 2, 1, 0 });

    private static ConcurrentRole 中段黄七揃い
        = new ConcurrentRole("中段黄七揃い", new Role[1] { 中段黄七 }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole 右上がり黄七揃い
        = new ConcurrentRole("右上がり黄七揃い", new Role[1] { 右上がり黄七 }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole 中段青七揃い
        = new ConcurrentRole("中段青七揃い", new Role[1] { 中段青七 }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole ハズレ揃い
        = new ConcurrentRole("ハズレ揃い", new Role[1] { ハズレ }, new int[3] { 0, 1, 2 });

    // Start is called before the first frame update
    void Start()
    {
        symbolIndexes = new SymbolName[3, 20]{
            {
                SymbolName.赤七,SymbolName.GOD,SymbolName.ハーデス,SymbolName.黄七,SymbolName.青七,
                SymbolName.赤七,SymbolName.GOD,SymbolName.ハーデス,SymbolName.黄七,SymbolName.青七,
                SymbolName.赤七,SymbolName.GOD,SymbolName.ハーデス,SymbolName.黄七,SymbolName.青七,
                SymbolName.赤七,SymbolName.GOD,SymbolName.ハーデス,SymbolName.黄七,SymbolName.青七,
            },
            {
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.青七,SymbolName.黄七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.青七,SymbolName.黄七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.青七,SymbolName.黄七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.青七,SymbolName.黄七,
            },
            {
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.黄七,SymbolName.青七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.黄七,SymbolName.青七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.黄七,SymbolName.青七,
                SymbolName.ハーデス,SymbolName.GOD,SymbolName.赤七,SymbolName.黄七,SymbolName.青七,
            },
        };
    }

    void DrawTextureOnRenderTexture(Texture2D sourceTexture, int x, int y, RenderTexture renderTexture)
    {
        // 現在のRenderTextureを保存
        RenderTexture currentRT = RenderTexture.active;

        // 作成したRenderTextureをアクティブに設定
        RenderTexture.active = renderTexture;

        // 画像描画の準備
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

        // 画像を描画
        Graphics.DrawTexture(new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture);
        GL.PopMatrix();

        // RenderTextureを元に戻す
        RenderTexture.active = currentRT;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X))
        {
            ConcurrentRole[] a = new ConcurrentRole[1] { 中段GOD揃い };
            roles = a[0];
            Spin();
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            StopReel(0);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            StopReel(1);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            StopReel(2);
        }

        SlipBiginControll(0);
        SlipBiginControll(1);
        SlipBiginControll(2);
        reelDrawUpdate(0);
        reelDrawUpdate(1);
        reelDrawUpdate(2);
    }

    private void reelDrawUpdate(int index)
    {
        int cellHeight = reelTexture[index].height / 20;
        DrawTextureOnRenderTexture(reelBackWhite, 0, 0, reelTexture[index]);
        for (int symbolI = 0; symbolI < 20; symbolI++)
        {
            Texture2D cSymbol = symbols[(int)symbolIndexes[index, symbolI]];
            DrawTextureOnRenderTexture(
                cSymbol,
                (reelTexture[index].width / 2) - (cSymbol.width / 2),
                (cellHeight / 2) - (cSymbol.height / 2) + symbolI * (cellHeight),
                reelTexture[index]);
        }
        DrawTextureOnRenderTexture(reelBackBlack, 0, 0, reelEmissonTexture[index]);
        for (int symbolI = 0; symbolI < 20; symbolI++)
        {
            Texture2D cSymbolMasks = symbols[(int)symbolIndexes[index, symbolI]];
            DrawTextureOnRenderTexture(
                cSymbolMasks,
                (reelTexture[index].width / 2) - (cSymbolMasks.width / 2),
                (cellHeight / 2) - (cSymbolMasks.height / 2) + symbolI * (cellHeight),
                reelEmissonTexture[index]);
        }
    }

    public void Spin()
    {
        reelStates[0] = ReelState.Spin;
        reelStates[1] = ReelState.Spin;
        reelStates[2] = ReelState.Spin;
        pushOrder = new int[3] { -1, -1, -1 };
        slipTypes = new SlipType[3] { SlipType.無, SlipType.無, SlipType.無 };
        stopedSymbolNames = new SymbolName[3] { SymbolName.NULL, SymbolName.NULL, SymbolName.NULL };
        pushCount = 0;
        if (roles.answerOrder != null)
        {
            answerOrder = roles.answerOrder;
        }
        isOrderMiss = false;
    }

    public void StopReel(int position)
    {
        if (reelStates[position] != ReelState.Spin)
        {
            return;
        }

        pushOrder[pushCount] = position;
        SymbolName[] searchSymbolNames;
        searchSymbolNames = new SymbolName[1] { SymbolName.NULL };

        if (roles.isFree)
        {
            // 押し順第一で決めるほう
            if (pushOrder[0] == 0)
            {
                searchSymbolNames = roles.freeRolesLeft.Select(r => r.symbols[position]).ToArray();
            }
            else if (pushOrder[0] == 1)
            {
                searchSymbolNames = roles.freeRolesCenter.Select(r => r.symbols[position]).ToArray();
            }
            else if (pushOrder[0] == 2)
            {
                searchSymbolNames = roles.freeRolesRight.Select(r => r.symbols[position]).ToArray();
            }
        }
        else
        {
            // 枚数優先か組み合わせ優先かを押し順によって決める
            if ((answerOrder[pushCount] == -1))
            {
                slipTypes[pushCount] = SlipType.払優;
            }
            else if (pushOrder[pushCount] == answerOrder[pushCount] && !isOrderMiss)
            {
                slipTypes[pushCount] = SlipType.払優;
            }
            else
            {
                slipTypes[pushCount] = SlipType.組優;
                isOrderMiss = true;
            }

            if (slipTypes[pushCount] == SlipType.組優)
            {
                // PayTypeごとにRoleをグループ化し、その出現回数をカウントする
                var payTypeCounts = roles.roles
                    .GroupBy(r => r.payType)
                    .Select(g => new { PayType = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                if (payTypeCounts.Any())
                {
                    // 最も多く出現したPayTypeを取得
                    PayType mostCommonPayType = payTypeCounts.First().PayType;
                    // そのPayTypeを持つRoleを取得
                    Role[] mostCommonRoles = roles.roles.Where(r => r.payType == mostCommonPayType).ToArray();
                    Role[] matchingRoles = mostCommonRoles.Where(r => MatchesCurrentSymbols(r.symbols, stopedSymbolNames)).ToArray();
                    searchSymbolNames = matchingRoles.Select(r => r.symbols[position]).ToArray();
                }
            }
            else if (slipTypes[pushCount] == SlipType.払優)
            {// 最大の払い出し枚数（PayTypeの最大値）を見つける
                PayType maxPayType = roles.roles.Max(r => r.payType);

                // 最大の払い出し枚数を持つRoleをすべて取得
                Role[] maxPayTypeRoles = roles.roles.Where(r => r.payType == maxPayType).ToArray();
                Role[] matchingRoles = maxPayTypeRoles.Where(r => MatchesCurrentSymbols(r.symbols, stopedSymbolNames)).ToArray();
                searchSymbolNames = matchingRoles.Select(r => r.symbols[position]).ToArray();
            }
        }
        pushCount++;
        reelStates[position] = ReelState.Slip;
        firstStopAngle[position] = angle[position];
        slipStopAngle[position] = (float)Mathf.Ceil(firstStopAngle[position] / komaAngle) * komaAngle % 360;
        // 滑り角度を計算するため図柄を探す
        bool isHited = false;
        for (int j = 0; j < searchSymbolNames.Length; j++)
        {
            for (int i = 0; i < 5; i++)
            {
                int lineIndex = (int)(20 - (angle[position] / komaAngle) + 1) - i;
                if ((SymbolName)symbolIndexes[position, (int)cramp(lineIndex, 20)] == searchSymbolNames[j])
                {
                    slipStopAngle[position] += (komaAngle * i);
                    stopedSymbolNames[position] = (SymbolName)symbolIndexes[position, (int)cramp(lineIndex, 20)];
                    isHited = true;
                    break;
                }
            }
            if (isHited)
            {
                Debug.Log(slipStopAngle[position]);
                break;
            }
        }
    }

    // 現在の停止されている図柄と、停止候補の
    private bool MatchesCurrentSymbols(SymbolName[] roleSymbols, SymbolName[] currentSymbols)
    {
        for (int i = 0; i < currentSymbols.Length; i++)
        {
            if (currentSymbols[i] != SymbolName.NULL && roleSymbols[i] != currentSymbols[i])
            {
                return false;
            }
        }
        return true;
    }
    private float cramp(float num, float cramp)
    {
        if (num < 0)
        {
            num += cramp;
        }
        else
        {
            num %= cramp;
        }
        return num;
    }
    public void SlipBiginControll(int position)
    {
        if (reelStates[position] == ReelState.Spin)
        {
            if (rollSpeeds[position] > MAX_SPIN_SPEED)
            {
                rollSpeeds[position] = MAX_SPIN_SPEED;
            }
            else
            {
                rollSpeeds[position] += 1.5f;
            }
        }
        else if (reelStates[position] == ReelState.Slip)
        {
            int offset = 0;
            if (angle[position] > 180)
            {
                offset = -360;
            }

            // スベリ０で図柄の位置をただしい場所へ
            // 停止位置が360を超えた場合のイレギュラー処理
            if ((angle[position] < 180 && slipStopAngle[position] >= 360) &&
                (angle[position] > slipStopAngle[position] - 360))
            {
                angle[position] = slipStopAngle[position];
                rollSpeeds[position] = 0f;
                reelStates[position] = ReelState.Stop;
            }
            else
            {
                if (angle[position] > slipStopAngle[position])
                {
                    angle[position] = slipStopAngle[position];
                    rollSpeeds[position] = 0f;
                    reelStates[position] = ReelState.Stop;
                }
            }

            // すべて停止したら
            if (reelStates[0] == ReelState.Stop &&
                reelStates[1] == ReelState.Stop &&
                reelStates[2] == ReelState.Stop)
            {
                // 出目チェック
                // CheckReel();
            }
        }
        angle[position] += rollSpeeds[position];
        angle[position] = cramp(angle[position], 360);
        if (position == 0)
        {
            Debug.Log(angle[position] +":" +slipStopAngle[position]);
        }
        
        reels[position].transform.eulerAngles = new Vector3(-angle[position], 0, 0);
    }
}
