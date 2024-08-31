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
    �n�[�f�X = 1,
    �� = 2,
    ���� = 3,
    �Ԏ� = 4,
}

public enum SlipType
{
    ��,
    �g�D,
    ���D
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

    private static Role ���iGOD = new Role("���iGOD", new SymbolName[3] { SymbolName.GOD, SymbolName.GOD, SymbolName.GOD }, PayType.Pay15);

    private static Role �N���b�V�� = new Role("�N���b�V��", new SymbolName[3] { SymbolName.��, SymbolName.����, SymbolName.�Ԏ� }, PayType.ReplayA);

    private static Role ���i�n�[�f�X = new Role("���i�n�[�f�X", new SymbolName[3] { SymbolName.�n�[�f�X, SymbolName.�n�[�f�X, SymbolName.�n�[�f�X }, PayType.ReplayA);
    private static Role ���i�n�[�f�X�t�F�C�N = new Role("���i�n�[�f�X�t�F�C�N", new SymbolName[3] { SymbolName.GOD, SymbolName.�n�[�f�X, SymbolName.�n�[�f�X }, PayType.ReplayA);

    private static Role ���i�Ԏ� = new Role("���i�Ԏ�", new SymbolName[3] { SymbolName.����, SymbolName.����, SymbolName.���� }, PayType.Pay15);
    private static Role ���i�Ԏ��t�F�C�N = new Role("���i�Ԏ��t�F�C�N", new SymbolName[3] { SymbolName.��, SymbolName.�Ԏ�, SymbolName.�Ԏ� }, PayType.Pay15);

    private static Role ���i���� = new Role("���i����", new SymbolName[3] { SymbolName.����, SymbolName.����, SymbolName.���� }, PayType.Pay15);

    private static Role �E�オ�艩�� = new Role("�E�オ�艩��", new SymbolName[3] { SymbolName.�n�[�f�X, SymbolName.����, SymbolName.�� }, PayType.ReplayA);

    private static Role ���i�� = new Role("���i��", new SymbolName[3] { SymbolName.��, SymbolName.��, SymbolName.�� }, PayType.ReplayA);

    private static Role �n�Y�� = new Role("�n�Y��", new SymbolName[3] { SymbolName.��, SymbolName.����, SymbolName.�� }, PayType.ReplayA);


    private static ConcurrentRole ���iGOD����
        = new ConcurrentRole("���iGOD����", new Role[1] { ���iGOD }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole �N���b�V������
        = new ConcurrentRole("�N���b�V������", new Role[1] { �N���b�V�� }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole �������n�[�f�X����231
        = new ConcurrentRole("�������n�[�f�X����231", new Role[2] { ���i�n�[�f�X, �n�Y�� }, new int[3] { 2, 0, 1 });
    private static ConcurrentRole �������n�[�f�X�n�Y������231
        = new ConcurrentRole("�������n�[�f�X�n�Y������231", new Role[2] { ���i�n�[�f�X�t�F�C�N, �n�Y�� }, new int[3] { 2, 0, 1 });

    private static ConcurrentRole �������Ԏ�����321
        = new ConcurrentRole("�������Ԏ�����321", new Role[2] { ���i�Ԏ�, �n�Y�� }, new int[3] { 2, 1, 0 });

    private static ConcurrentRole �������Ԏ��n�Y������321
        = new ConcurrentRole("�������Ԏ��n�Y������321", new Role[2] { ���i�Ԏ��t�F�C�N, �n�Y�� }, new int[3] { 2, 1, 0 });

    private static ConcurrentRole ���i��������
        = new ConcurrentRole("���i��������", new Role[1] { ���i���� }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole �E�オ�艩������
        = new ConcurrentRole("�E�オ�艩������", new Role[1] { �E�オ�艩�� }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole ���i������
        = new ConcurrentRole("���i������", new Role[1] { ���i�� }, new int[3] { 0, 1, 2 });

    private static ConcurrentRole �n�Y������
        = new ConcurrentRole("�n�Y������", new Role[1] { �n�Y�� }, new int[3] { 0, 1, 2 });

    // Start is called before the first frame update
    void Start()
    {
        symbolIndexes = new SymbolName[3, 20]{
            {
                SymbolName.�Ԏ�,SymbolName.GOD,SymbolName.�n�[�f�X,SymbolName.����,SymbolName.��,
                SymbolName.�Ԏ�,SymbolName.GOD,SymbolName.�n�[�f�X,SymbolName.����,SymbolName.��,
                SymbolName.�Ԏ�,SymbolName.GOD,SymbolName.�n�[�f�X,SymbolName.����,SymbolName.��,
                SymbolName.�Ԏ�,SymbolName.GOD,SymbolName.�n�[�f�X,SymbolName.����,SymbolName.��,
            },
            {
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.��,SymbolName.����,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.��,SymbolName.����,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.��,SymbolName.����,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.��,SymbolName.����,
            },
            {
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.����,SymbolName.��,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.����,SymbolName.��,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.����,SymbolName.��,
                SymbolName.�n�[�f�X,SymbolName.GOD,SymbolName.�Ԏ�,SymbolName.����,SymbolName.��,
            },
        };
    }

    void DrawTextureOnRenderTexture(Texture2D sourceTexture, int x, int y, RenderTexture renderTexture)
    {
        // ���݂�RenderTexture��ۑ�
        RenderTexture currentRT = RenderTexture.active;

        // �쐬����RenderTexture���A�N�e�B�u�ɐݒ�
        RenderTexture.active = renderTexture;

        // �摜�`��̏���
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

        // �摜��`��
        Graphics.DrawTexture(new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture);
        GL.PopMatrix();

        // RenderTexture�����ɖ߂�
        RenderTexture.active = currentRT;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X))
        {
            ConcurrentRole[] a = new ConcurrentRole[1] { ���iGOD���� };
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
        slipTypes = new SlipType[3] { SlipType.��, SlipType.��, SlipType.�� };
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
            // ���������Ō��߂�ق�
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
            // �����D�悩�g�ݍ��킹�D�悩���������ɂ���Č��߂�
            if ((answerOrder[pushCount] == -1))
            {
                slipTypes[pushCount] = SlipType.���D;
            }
            else if (pushOrder[pushCount] == answerOrder[pushCount] && !isOrderMiss)
            {
                slipTypes[pushCount] = SlipType.���D;
            }
            else
            {
                slipTypes[pushCount] = SlipType.�g�D;
                isOrderMiss = true;
            }

            if (slipTypes[pushCount] == SlipType.�g�D)
            {
                // PayType���Ƃ�Role���O���[�v�����A���̏o���񐔂��J�E���g����
                var payTypeCounts = roles.roles
                    .GroupBy(r => r.payType)
                    .Select(g => new { PayType = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                if (payTypeCounts.Any())
                {
                    // �ł������o������PayType���擾
                    PayType mostCommonPayType = payTypeCounts.First().PayType;
                    // ����PayType������Role���擾
                    Role[] mostCommonRoles = roles.roles.Where(r => r.payType == mostCommonPayType).ToArray();
                    Role[] matchingRoles = mostCommonRoles.Where(r => MatchesCurrentSymbols(r.symbols, stopedSymbolNames)).ToArray();
                    searchSymbolNames = matchingRoles.Select(r => r.symbols[position]).ToArray();
                }
            }
            else if (slipTypes[pushCount] == SlipType.���D)
            {// �ő�̕����o�������iPayType�̍ő�l�j��������
                PayType maxPayType = roles.roles.Max(r => r.payType);

                // �ő�̕����o������������Role�����ׂĎ擾
                Role[] maxPayTypeRoles = roles.roles.Where(r => r.payType == maxPayType).ToArray();
                Role[] matchingRoles = maxPayTypeRoles.Where(r => MatchesCurrentSymbols(r.symbols, stopedSymbolNames)).ToArray();
                searchSymbolNames = matchingRoles.Select(r => r.symbols[position]).ToArray();
            }
        }
        pushCount++;
        reelStates[position] = ReelState.Slip;
        firstStopAngle[position] = angle[position];
        slipStopAngle[position] = (float)Mathf.Ceil(firstStopAngle[position] / komaAngle) * komaAngle % 360;
        // ����p�x���v�Z���邽�ߐ}����T��
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

    // ���݂̒�~����Ă���}���ƁA��~����
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

            // �X�x���O�Ő}���̈ʒu�����������ꏊ��
            // ��~�ʒu��360�𒴂����ꍇ�̃C���M�����[����
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

            // ���ׂĒ�~������
            if (reelStates[0] == ReelState.Stop &&
                reelStates[1] == ReelState.Stop &&
                reelStates[2] == ReelState.Stop)
            {
                // �o�ڃ`�F�b�N
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
