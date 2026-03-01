-- filename: 
-- version: lua51
-- line: [0, 0] id: 0
g_GAME_VISION = SKY_Get(VAR_APP, PROP_APP_GAME_VERSION)
dskLogin = CreateDesktop("dskLogin", "login_desktop_proc")
dskSelect = CreateDesktop("dskSelect", "select_desktop_proc")
job_level = {}
chaname = {}
frmLogin = 0
edtAccount = 0
edtPassWord = 0
keyboard = 0
frmSelect = 0
d3dImage0 = 0
d3dImage1 = 0
d3dImage2 = 0
dianxin1_ip = {
  [1] = "61.160.200.140",
  [2] = "61.160.200.141",
  [3] = "61.160.200.142",
  [4] = "61.160.200.143",
}
dianxin2_ip = {
  [1] = "61.160.200.150",
  [2] = "61.160.200.151",
  [3] = "61.160.200.152",
  [4] = "61.160.200.153",
}
dianxin3_ip = {
  [1] = "61.160.200.174",
  [2] = "61.160.200.175",
  [3] = "61.160.200.174",
  [4] = "61.160.200.175",
}
wangtong1_ip = {
  [1] = "60.217.234.235",
  [2] = "60.217.234.236",
  [3] = "60.217.234.237",
  [4] = "60.217.234.238",
}
wangtong2_ip = {
  [1] = "60.217.234.231",
  [2] = "60.217.234.244",
  [3] = "60.217.234.245",
  [4] = "60.217.234.246",
}
test_1 = "210.14.67.21"
test_2 = "210.14.67.22"
test_ip = {
  [1] = "61.160.200.140",
  [2] = "61.160.200.141",
  [3] = "61.160.200.142",
  [4] = "61.160.200.143",
  [5] = "61.160.200.150",
  [6] = "61.160.200.151",
  [7] = "61.160.200.152",
  [8] = "61.160.200.153",
}
net_count = 3
SD_login_acc = 0
server_table = {}
Select = {
  Shot = {
    0,
    0,
    0,
    0,
    0
  },
  Circle = {
    0,
    0,
    0,
    0,
    0
  },
  TypeId = {
    0,
    0,
    0,
    0,
    0
  },
  Sign = 0,
  count = 0,
  clear = function(r0_1)
    -- line: [51, 63] id: 1
    local r1_1 = 0
    for r5_1 = 1, 5, 1 do
      SKY_Set(Select.Circle[r5_1], PROP_UI_SHOW, VAR_FALSE)
      Select.TypeId[r5_1] = 0
      if SKY_Get(chaname[r5_1], PROP_UI_TEXT) == "" then
        SKY_Set(chaname[r5_1], PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1000))
        SKY_Set(job_level[r5_1], PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1001))
      end
    end
    Select.count = 0
  end,
}
agreement_tag = 1
select_play_ok_button = {
  button = 0,
  [1] = {
    x = 303,
    y = 105,
  },
  [2] = {
    x = 303,
    y = 169,
  },
  [3] = {
    x = 303,
    y = 233,
  },
  [4] = {
    x = 303,
    y = 298,
  },
  [5] = {
    x = 303,
    y = 362,
  },
}
function login_desktop_proc(r0_2, r1_2, r2_2)
  -- line: [73, 96] id: 2
  if r1_2 == MSG_UI_DESKTOP_ACTIVE then
    SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC_STOP)
    SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC, "music/mp3/1.mp3", VAR_TRUE)
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(frmLogin_back, PROP_FORM_SHOW, VAR_TRUE)
    SKY_Set(frmLogin, PROP_FORM_SHOW, VAR_TRUE)
    SKY_Set(edtAccount, PROP_UI_TEXT, "")
    SKY_Set(edtPassWord, PROP_UI_TEXT, "")
    SKY_Set(frmLogin, PROP_FORM_ACTIVE, edtAccount)
    SKY_Set(VAR_APP, PROP_APP_SWITCH_SCENE, VAR_APP_SCENE_NULL)
    if agreement_tag == 1 then
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
      SKY_Set(frmback_storyform_imge, PROP_FORM_SHOW, VAR_TRUE, VAR_FALSE)
      SKY_Set(back_storyform, PROP_FORM_SHOW, VAR_TRUE)
      agreement_tag = 0
    end
  end
end
function select_desktop_proc(r0_3, r1_3, r2_3)
  -- line: [98, 111] id: 3
  if r1_3 == MSG_UI_DESKTOP_ACTIVE then
    SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC_STOP)
    SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC, "music/mp3/2.mp3", VAR_TRUE)
    SKY_Set(selectformback, PROP_FORM_SHOW, VAR_TRUE)
    SKY_Set(frmSelect, PROP_FORM_SHOW, VAR_TRUE)
    lodingn_gamepage_boy_tag = 1
    SKY_Set(VAR_APP, PROP_APP_SWITCH_SCENE, VAR_APP_SCENE_SELECT)
  elseif r1_3 == MSG_UI_DESKTOP_LOST then
    Select:clear()
  end
end
function CreateLoginFormBack()
  -- line: [112, 153] id: 4
  local r0_4 = SKY_Create(TYPE_UI_FORM, "frmLogin_back")
  SKY_Set(r0_4, PROP_PROC, "login_back_form_proc")
  SKY_Set(r0_4, PROP_FORM_DESKTOP, dskLogin)
  frmLogin_back = r0_4
  SKY_Set(r0_4, PROP_UI_POS, 0, 0)
  SKY_Set(r0_4, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  SKY_Set(r0_4, PROP_UI_ENABLED, VAR_FALSE)
  local r1_4 = SKY_Create(TYPE_UI_IMAGE, "imgback")
  SKY_Set(r0_4, PROP_FORM_ADD, r1_4)
  SKY_Set(r1_4, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  storyform_imgback = r1_4
  if g_GAME_VISION == VAR_APP_GAME_SHUN then
    SKYImageLoad(SKY_Get(r1_4, PROP_UI_IMAGE), "login\\back_imagesun.dds", 1024, 768, 0, 0)
  elseif g_GAME_VISION == VAR_APP_GAME_17173 then
    SKYImageLoad(SKY_Get(r1_4, PROP_UI_IMAGE), "login\\back_image17173.dds", 1024, 768, 0, 0)
  elseif g_GAME_VISION == VAR_APP_GAME_MOQI then
    SKYImageLoad(SKY_Get(r1_4, PROP_UI_IMAGE), "login\\back_imagemoqi.dds", 1024, 768, 0, 0)
  else
    SKYImageLoad(SKY_Get(r1_4, PROP_UI_IMAGE), "login\\back_image.dds", 1024, 768, 0, 0)
  end
  local r2_4 = SKY_Create(TYPE_UI_IMAGE, "imgkuang")
  SKY_Set(r0_4, PROP_FORM_ADD, r2_4)
  SKY_Set(r2_4, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  storyform_imgkuang = r2_4
  SKYImageLoad(SKY_Get(r2_4, PROP_UI_IMAGE), "login\\waikuang_image.dds", 1024, 768, 0, 0)
end
CreateLoginFormBack()
function login_back_form_proc()
  -- line: [156, 157] id: 5
end
function CreateDefaultLoginForm()
  -- line: [159, 548] id: 6
  local r0_6 = SKY_Create(TYPE_UI_FORM, "frmLogin")
  SKY_Set(r0_6, PROP_PROC, "login_form_proc")
  frmLogin = r0_6
  SKY_Set(r0_6, PROP_FORM_DESKTOP, dskLogin)
  SKY_Set(r0_6, PROP_UI_POS, 0, 0, 102, 16)
  SKY_Set(r0_6, PROP_UI_SIZE, 1024, 1024)
  local r1_6 = SKY_Create(TYPE_UI_IMAGE, "imgaccountblack")
  SKY_Set(r0_6, PROP_FORM_ADD, r1_6)
  SKY_Set(r1_6, PROP_UI_SIZE, 265, 362)
  SKY_Set(r1_6, PROP_UI_POS, 381, 122)
  SKYImageLoad(SKY_Get(r1_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 265, 362, 645, 0)
  local r2_6 = SKY_Create(TYPE_UI_IMAGE, "imgxuanqu")
  SKY_Set(r0_6, PROP_FORM_ADD, r2_6)
  SKY_Set(r2_6, PROP_UI_SIZE, 323, 485)
  SKY_Set(r2_6, PROP_UI_POS, 677, 112)
  SKYImageLoad(SKY_Get(r2_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 323, 490, 325, 0)
  local r3_6 = SKY_Create(TYPE_UI_IMAGE, "imgBoard")
  SKY_Set(r0_6, PROP_FORM_ADD, r3_6)
  SKY_Set(r3_6, PROP_UI_SIZE, 320, 490)
  SKY_Set(r3_6, PROP_UI_POS, 30, 113)
  SKYImageLoad(SKY_Get(r3_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 320, 490, 0, 0)
  local r4_6 = SKY_Create(TYPE_UI_LABEL, "versionlabel")
  SKY_Set(r0_6, PROP_FORM_ADD, r4_6)
  SKY_Set(r4_6, PROP_UI_POS, 517, 276)
  SKY_Set(r4_6, PROP_UI_SIZE, 115, 20)
  SKY_Set(r4_6, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r4_6, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r4_6, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1002) .. VAR_VERSION)
  local r5_6 = SKY_Create(TYPE_UI_LIST, "serverchang")
  SKY_Set(r0_6, PROP_FORM_ADD, r5_6)
  SKY_Set(r5_6, PROP_UI_POS, 710, 197)
  SKY_Set(r5_6, PROP_UI_SIZE, 279, 244)
  SKY_Set(r5_6, PROP_UI_COLOR, 255, 217, 214, 199)
  SKY_Set(SKY_Get(r5_6, PROP_UI_LIST_ITEM), PROP_ITEM_LIST_FONT, DEFALUT_FONT3)
  SKY_Set(r5_6, PROP_UI_MARGIN, 0, 35, 25, 0)
  local r6_6 = SKY_Create(TYPE_UI_SCROLL, "questtalkscroll")
  SKY_Set(r5_6, PROP_UI_LIST_SCROLL, r6_6)
  SKY_Set(r6_6, PROP_UI_POS, 203, 2)
  SKY_Set(r6_6, PROP_UI_SIZE, 22, 244)
  SKY_Set(r6_6, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r6_6, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r6_6, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r6_6, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r6_6, PROP_UI_SCROLL_VALUE, 1)
  local r7_6 = SKY_Create(TYPE_UI_IMAGE, "imgblack")
  SKY_Set(r6_6, PROP_UI_INSERT_CHILD, r7_6)
  SKY_Set(r7_6, PROP_UI_SIZE, 28, 106)
  SKY_Set(r7_6, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r7_6 = SKY_Get(r7_6, PROP_UI_IMAGE)
  local r8_6 = SKY_Get(r6_6, PROP_UI_SCROLL_UP)
  SKY_Set(r8_6, PROP_UI_SIZE, 22, 19)
  SKY_Set(r8_6, PROP_UI_POS, 0, 0)
  local r9_6 = SKY_Get(r6_6, PROP_UI_SCROLL_DOWN)
  SKY_Set(r9_6, PROP_UI_SIZE, 22, 19)
  SKY_Set(r9_6, PROP_UI_POS, 0, 219)
  local r10_6 = SKY_Get(r6_6, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r10_6, PROP_UI_SIZE, 17, 41)
  SKYImageLoad(SKY_Get(r10_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 17, 41, 0, 652)
  local r12_6 = SKY_Get(r5_6, PROP_UI_LIST_ITEM)
  SKY_Set(r12_6, PROP_UI_SIZE, 0, 1)
  SKY_Set(r12_6, PROP_ITEM_LIST_SELECT_OFFSET, 0, -5)
  SKY_Set(r12_6, PROP_ITEM_LIST_FOLLOW, VAR_FALSE)
  SKY_Set(r12_6, PROP_ITEM_LIST_UNIT_SPACE, -4)
  item_select = r12_6
  local r13_6 = SKY_Get(r12_6, PROP_ITEM_LIST_SELECT_PIC)
  SKY_Set(r13_6, PROP_UI_SIZE, 0, 1)
  SKY_Set(r13_6, PROP_UI_COLOR, 100, 255, 191, 0)
  SKY_Set(r13_6, PROP_IMAGE_LOAD, "system\\white.tga", 0, 1)
  local r14_6 = SKY_Create(TYPE_UI_MEMO, "systeminfo")
  SKY_Set(r0_6, PROP_FORM_ADD, r14_6)
  SKY_Set(r14_6, PROP_UI_POS, 45, 126)
  SKY_Set(r14_6, PROP_UI_SIZE, 275, 373)
  SKY_Set(r14_6, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_6, PROP_UI_MEMO_TEXT_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_6, PROP_UI_MEMO_TEXT_FONT, DEFALUT_FONT3)
  SKY_Set(r14_6, PROP_UI_MARGIN, 24, 25, 40, 25)
  SKY_Set(r14_6, PROP_UI_MEMO_LINE_SPACE, 1)
  SKY_Set(r14_6, PROP_UI_MEMO_TEXT_SHADE, VAR_FALSE)
  systeminfo = r14_6
  local r15_6 = SKY_Create(TYPE_UI_SCROLL, "systeminfoscroll")
  SKY_Set(r14_6, PROP_UI_MEMO_SCROLL, r15_6)
  SKY_Set(r15_6, PROP_UI_POS, 203, 2)
  SKY_Set(r15_6, PROP_UI_SIZE, 22, 373)
  SKY_Set(r15_6, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r15_6, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r15_6, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r15_6, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r15_6, PROP_UI_SCROLL_VALUE, 1)
  local r16_6 = SKY_Create(TYPE_UI_IMAGE, "systeminfo_imgblack")
  SKY_Set(r15_6, PROP_UI_INSERT_CHILD, r16_6)
  SKY_Set(r16_6, PROP_UI_SIZE, 28, 106)
  SKY_Set(r16_6, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r16_6 = SKY_Get(r16_6, PROP_UI_IMAGE)
  local r17_6 = SKY_Get(r15_6, PROP_UI_SCROLL_UP)
  SKY_Set(r17_6, PROP_UI_SIZE, 22, 15)
  SKY_Set(r17_6, PROP_UI_POS, 0, 0)
  local r18_6 = SKY_Get(r15_6, PROP_UI_SCROLL_DOWN)
  SKY_Set(r18_6, PROP_UI_SIZE, 22, 19)
  SKY_Set(r18_6, PROP_UI_POS, 0, 355)
  local r19_6 = SKY_Get(r15_6, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r19_6, PROP_UI_SIZE, 17, 40)
  SKYImageLoad(SKY_Get(r19_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 17, 40, 0, 652)
  edtAccount = CreateEdit(r0_6, "edtAccount", 177, 27, 456, 301)
  edtPassWord = CreateEdit(r0_6, "edtPassWord", 177, 27, 456, 332)
  SKY_Set(edtPassWord, PROP_UI_EDIT_PASSWORD, "*")
  local r21_6 = CreateButton(r0_6, "btnLogin", 74, 23, 436, 360)
  LoadButton(r21_6, "login/back_yuanshu.dds", 74, 23, 693, 418, 0)
  LoadButton(r21_6, "login/back_yuanshu.dds", 74, 23, 693, 455, 1)
  LoadButton(r21_6, "login/back_yuanshu.dds", 74, 23, 693, 455, 2)
  LoadButton(r21_6, "login/back_yuanshu.dds", 74, 23, 693, 455, 3)
  local r22_6 = CreateButton(r0_6, "btnExit", 74, 23, 519, 360)
  LoadButton(r22_6, "login/back_yuanshu.dds", 74, 23, 785, 418, 0)
  LoadButton(r22_6, "login/back_yuanshu.dds", 74, 23, 785, 455, 1)
  LoadButton(r22_6, "login/back_yuanshu.dds", 74, 23, 785, 455, 2)
  LoadButton(r22_6, "login/back_yuanshu.dds", 74, 23, 785, 455, 3)
  SKY_Set(r0_6, PROP_FORM_ESC_BUTTON, r22_6)
  local r23_6 = CreateButton(r0_6, "btnDragon", 98, 18, 743, 172)
  local r24_6 = CreateButton(r0_6, "btnlegend", 98, 18, 855, 172)
  local r25_6 = SKY_Create(TYPE_UI_IMAGE, "Dragon_btn")
  SKY_Set(r0_6, PROP_FORM_ADD, r25_6)
  SKY_Set(r25_6, PROP_UI_SIZE, 98, 18)
  SKY_Set(r25_6, PROP_UI_POS, 743, 172)
  SKYImageLoad(SKY_Get(r25_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 416, 572)
  local r26_6 = SKY_Create(TYPE_UI_IMAGE, "legend_btn")
  SKY_Set(r0_6, PROP_FORM_ADD, r26_6)
  SKY_Set(r26_6, PROP_UI_SIZE, 98, 18)
  SKY_Set(r26_6, PROP_UI_POS, 855, 172)
  SKYImageLoad(SKY_Get(r26_6, PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 516, 549)
  g_igw = SKY_Create(TYPE_UI_IGW, "igw")
  SKY_Set(g_igw, PROP_UI_POS, 0, 0)
  SKY_Set(g_igw, PROP_UI_SIZE, 10, 10)
  local r27_6 = SKY_Create(TYPE_UI_CHECK_BOX, "saveAccount")
  SKY_Set(r0_6, PROP_FORM_ADD, r27_6)
  SKY_Set(r27_6, PROP_UI_POS, 456, 420)
  SKY_Set(r27_6, PROP_UI_SIZE, 16, 17)
  SKY_Set(r27_6, PROP_UI_CHECK_ICON_SIZE, 16, 17)
  SKYImageLoad(SKY_Get(r27_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r27_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK_HOVER), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r27_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login/back_yuanshu.dds", 16, 17, 718, 395)
  SKYImageLoad(SKY_Get(r27_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login/back_yuanshu.dds", 16, 17, 718, 395)
  local r29_6 = SKY_Create(TYPE_UI_COMBO_BOX, "servercombox")
  SKY_Set(r0_6, PROP_FORM_ADD, r29_6)
  SKY_Set(r29_6, PROP_UI_POS, 834, 135)
  SKY_Set(r29_6, PROP_UI_SIZE, 111, 22)
  SKY_Set(r29_6, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r29_6, PROP_UI_COMBO_BOX_TEXT_POS, 24, 2)
  SKY_Set(r29_6, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r29_6, PROP_UI_FONT, DEFALUT_FONT3)
  login_combo_server_name = r29_6
  local r30_6 = SKY_Get(r29_6, PROP_UI_COMBO_BOX_MENU)
  login_combo_server_name = r30_6
  local r31_6 = SKY_Get(r30_6, PROP_UI_IMAGE)
  SKY_Set(r31_6, PROP_ITEM_RECT_STYLE, VAR_ITEM_RECT_FILL)
  SKY_Set(r31_6, PROP_UI_COLOR, 255, 0, 0, 0)
  local r32_6 = SKY_Get(r29_6, PROP_UI_COMBO_BOX_MENU)
  local r33_6 = SKY_Get(SKY_Get(r32_6, PROP_UI_MENU_ITEM), PROP_MENU_GROUP_SELECT_PIC)
  SKY_Set(r33_6, PROP_UI_COLOR, 255, 61, 61, 61)
  SKY_Set(r33_6, PROP_ITEM_RECT_COLOR, 255, 61, 61, 61)
  local r34_6 = SKY_Get(r29_6, PROP_UI_COMBO_BOX_BUTTON)
  SKY_Set(r34_6, PROP_UI_SIZE, 18, 19)
  SKY_Set(r34_6, PROP_UI_ALIGN, VAR_UI_ALIGN_LEFT_UP)
  local r35_6 = SKY_Get(r32_6, PROP_UI_MENU_ITEM)
  local r36_6 = SKY_Create(TYPE_UI_IMAGE, "imgFormImage")
  SKY_Set(r0_6, PROP_FORM_ADD, r36_6)
  SKY_Set(r36_6, PROP_UI_SIZE, 324, 290)
  SKY_Set(r36_6, PROP_UI_POS, 350, 325)
  SKY_Set(r36_6, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r36_6, PROP_UI_IMAGE), "login\\keyboard.dds", 324, 290, 0, 0)
  local r37_6 = SKY_Create(TYPE_UI_KEYBOARD, "keyboard")
  SKY_Set(r0_6, PROP_FORM_ADD, r37_6)
  SKY_Set(r37_6, PROP_UI_POS, 363, 408)
  SKY_Set(r37_6, PROP_UI_SIZE, 324, 260)
  SKY_Set(r37_6, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r37_6, PROP_UI_KBD_MAX_CHA, 20)
  SKY_Set(r37_6, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r37_6, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r37_6, PROP_UI_KBD_LOAD, "login\\keyboard.dds", 5, 10, 30, 27, 3, 3)
  keyboard = r37_6
  local r38_6 = SKY_Create(TYPE_UI_LABEL, "pasdlabel")
  SKY_Set(r0_6, PROP_FORM_ADD, r38_6)
  SKY_Set(r38_6, PROP_UI_POS, 403, 370)
  SKY_Set(r38_6, PROP_UI_SIZE, 200, 20)
  SKY_Set(r38_6, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r38_6, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r38_6, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r38_6, PROP_UI_SHOW, VAR_FALSE)
  local r39_6 = CreateButton(r0_6, "btncapitalization", 20, 20, 455, 461)
  SKY_Set(r39_6, PROP_UI_SHOW, VAR_TRUE)
  SKY_Set(r39_6, PROP_UI_HINT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1003))
  local r40_6 = CreateButton(r0_6, "btnaccount", 88, 28, 375, 579)
  SKY_Set(r40_6, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r40_6, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r40_6, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r40_6, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 25, 297)
  local r42_6 = CreateButton(r0_6, "btndelet", 60, 30, 545, 536)
  SKYImageLoad(SKY_Get(r42_6, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 60, 30, 241, 296)
  SKYImageLoad(SKY_Get(r42_6, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 60, 30, 241, 296)
  SKY_Set(r42_6, PROP_UI_SHOW, VAR_FALSE)
  local r43_6 = CreateButton(r0_6, "btnclose", 88, 28, 468, 579)
  SKY_Set(r43_6, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r43_6, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r43_6, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r43_6, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 118, 297)
  local r45_6 = SKY_Create(TYPE_UI_CHECK_BOX, "btnkeyboard")
  SKY_Set(r0_6, PROP_FORM_ADD, r45_6)
  SKY_Set(r45_6, PROP_UI_POS, 569, 585)
  SKY_Set(r45_6, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r45_6, PROP_UI_SIZE, 17, 17)
  SKY_Set(r45_6, PROP_UI_CHECK_ICON_SIZE, 17, 17)
  SKYImageLoad(SKY_Get(r45_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login\\keyboard.dds", 17, 17, 219, 302)
  SKYImageLoad(SKY_Get(r45_6, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login\\keyboard.dds", 17, 17, 219, 302)
end
function CreateSunLoginForm()
  -- line: [550, 943] id: 7
  local r0_7 = SKY_Create(TYPE_UI_FORM, "frmLogin")
  SKY_Set(r0_7, PROP_PROC, "login_form_proc")
  frmLogin = r0_7
  SKY_Set(r0_7, PROP_FORM_DESKTOP, dskLogin)
  SKY_Set(r0_7, PROP_UI_POS, 0, 0, 102, 16)
  SKY_Set(r0_7, PROP_UI_SIZE, 1024, 1024)
  local r1_7 = SKY_Create(TYPE_UI_IMAGE, "imgaccountblack")
  SKY_Set(r0_7, PROP_FORM_ADD, r1_7)
  SKY_Set(r1_7, PROP_UI_SIZE, 276, 288)
  SKY_Set(r1_7, PROP_UI_POS, 381, 122)
  SKYImageLoad(SKY_Get(r1_7, PROP_UI_IMAGE), "login/back_yuanshusun.dds", 276, 288, 662, 511)
  local r2_7 = SKY_Create(TYPE_UI_IMAGE, "imgxuanqu")
  SKY_Set(r0_7, PROP_FORM_ADD, r2_7)
  SKY_Set(r2_7, PROP_UI_SIZE, 323, 485)
  SKY_Set(r2_7, PROP_UI_POS, 677, 112)
  SKYImageLoad(SKY_Get(r2_7, PROP_UI_IMAGE), "login/back_yuanshusun.dds", 323, 490, 325, 0)
  local r3_7 = SKY_Create(TYPE_UI_IMAGE, "imgBoard")
  SKY_Set(r0_7, PROP_FORM_ADD, r3_7)
  SKY_Set(r3_7, PROP_UI_SIZE, 320, 490)
  SKY_Set(r3_7, PROP_UI_POS, 30, 113)
  SKYImageLoad(SKY_Get(r3_7, PROP_UI_IMAGE), "login/back_yuanshusun.dds", 320, 490, 0, 0)
  local r4_7 = SKY_Create(TYPE_UI_LABEL, "versionlabel")
  SKY_Set(r0_7, PROP_FORM_ADD, r4_7)
  SKY_Set(r4_7, PROP_UI_POS, 517, 290)
  SKY_Set(r4_7, PROP_UI_SIZE, 115, 20)
  SKY_Set(r4_7, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r4_7, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r4_7, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1004) .. VAR_VERSION)
  local r5_7 = SKY_Create(TYPE_UI_LIST, "serverchang")
  SKY_Set(r0_7, PROP_FORM_ADD, r5_7)
  SKY_Set(r5_7, PROP_UI_POS, 710, 197)
  SKY_Set(r5_7, PROP_UI_SIZE, 279, 244)
  SKY_Set(r5_7, PROP_UI_COLOR, 255, 217, 214, 199)
  SKY_Set(SKY_Get(r5_7, PROP_UI_LIST_ITEM), PROP_ITEM_LIST_FONT, DEFALUT_FONT3)
  SKY_Set(r5_7, PROP_UI_MARGIN, 0, 35, 25, 0)
  local r6_7 = SKY_Create(TYPE_UI_SCROLL, "questtalkscroll")
  SKY_Set(r5_7, PROP_UI_LIST_SCROLL, r6_7)
  SKY_Set(r6_7, PROP_UI_POS, 203, 2)
  SKY_Set(r6_7, PROP_UI_SIZE, 22, 244)
  SKY_Set(r6_7, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r6_7, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r6_7, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r6_7, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r6_7, PROP_UI_SCROLL_VALUE, 1)
  local r7_7 = SKY_Create(TYPE_UI_IMAGE, "imgblack")
  SKY_Set(r6_7, PROP_UI_INSERT_CHILD, r7_7)
  SKY_Set(r7_7, PROP_UI_SIZE, 28, 106)
  SKY_Set(r7_7, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r7_7 = SKY_Get(r7_7, PROP_UI_IMAGE)
  local r8_7 = SKY_Get(r6_7, PROP_UI_SCROLL_UP)
  SKY_Set(r8_7, PROP_UI_SIZE, 22, 19)
  SKY_Set(r8_7, PROP_UI_POS, 0, 0)
  local r9_7 = SKY_Get(r6_7, PROP_UI_SCROLL_DOWN)
  SKY_Set(r9_7, PROP_UI_SIZE, 22, 19)
  SKY_Set(r9_7, PROP_UI_POS, 0, 219)
  local r10_7 = SKY_Get(r6_7, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r10_7, PROP_UI_SIZE, 17, 41)
  SKYImageLoad(SKY_Get(r10_7, PROP_UI_IMAGE), "login/back_yuanshusun.dds", 17, 41, 0, 652)
  local r12_7 = SKY_Get(r5_7, PROP_UI_LIST_ITEM)
  SKY_Set(r12_7, PROP_UI_SIZE, 0, 1)
  SKY_Set(r12_7, PROP_ITEM_LIST_SELECT_OFFSET, 0, -5)
  SKY_Set(r12_7, PROP_ITEM_LIST_FOLLOW, VAR_FALSE)
  SKY_Set(r12_7, PROP_ITEM_LIST_UNIT_SPACE, -4)
  item_select = r12_7
  local r13_7 = SKY_Get(r12_7, PROP_ITEM_LIST_SELECT_PIC)
  SKY_Set(r13_7, PROP_UI_SIZE, 0, 1)
  SKY_Set(r13_7, PROP_UI_COLOR, 100, 255, 191, 0)
  SKY_Set(r13_7, PROP_IMAGE_LOAD, "system\\white.tga", 0, 1)
  local r14_7 = SKY_Create(TYPE_UI_MEMO, "systeminfo")
  SKY_Set(r0_7, PROP_FORM_ADD, r14_7)
  SKY_Set(r14_7, PROP_UI_POS, 45, 126)
  SKY_Set(r14_7, PROP_UI_SIZE, 275, 373)
  SKY_Set(r14_7, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_7, PROP_UI_MEMO_TEXT_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_7, PROP_UI_MEMO_TEXT_FONT, DEFALUT_FONT3)
  SKY_Set(r14_7, PROP_UI_MARGIN, 24, 25, 40, 25)
  SKY_Set(r14_7, PROP_UI_MEMO_LINE_SPACE, 1)
  SKY_Set(r14_7, PROP_UI_MEMO_TEXT_SHADE, VAR_FALSE)
  systeminfo = r14_7
  local r15_7 = SKY_Create(TYPE_UI_SCROLL, "systeminfoscroll")
  SKY_Set(r14_7, PROP_UI_MEMO_SCROLL, r15_7)
  SKY_Set(r15_7, PROP_UI_POS, 203, 2)
  SKY_Set(r15_7, PROP_UI_SIZE, 22, 373)
  SKY_Set(r15_7, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r15_7, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r15_7, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r15_7, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r15_7, PROP_UI_SCROLL_VALUE, 1)
  local r16_7 = SKY_Create(TYPE_UI_IMAGE, "systeminfo_imgblack")
  SKY_Set(r15_7, PROP_UI_INSERT_CHILD, r16_7)
  SKY_Set(r16_7, PROP_UI_SIZE, 28, 106)
  SKY_Set(r16_7, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r16_7 = SKY_Get(r16_7, PROP_UI_IMAGE)
  local r17_7 = SKY_Get(r15_7, PROP_UI_SCROLL_UP)
  SKY_Set(r17_7, PROP_UI_SIZE, 22, 15)
  SKY_Set(r17_7, PROP_UI_POS, 0, 0)
  local r18_7 = SKY_Get(r15_7, PROP_UI_SCROLL_DOWN)
  SKY_Set(r18_7, PROP_UI_SIZE, 22, 19)
  SKY_Set(r18_7, PROP_UI_POS, 0, 355)
  local r19_7 = SKY_Get(r15_7, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r19_7, PROP_UI_SIZE, 17, 40)
  SKYImageLoad(SKY_Get(r19_7, PROP_UI_IMAGE), "login/back_yuanshusun.dds", 17, 40, 0, 652)
  edtAccount = CreateEdit(r0_7, "edtAccount", 0, 0, 1, 1)
  SKY_Set(edtAccount, PROP_UI_SHOW, VAR_FALSE)
  edtPassWord = CreateEdit(r0_7, "edtPassWord", 0, 0, 1, 1)
  SKY_Set(edtPassWord, PROP_UI_SHOW, VAR_FALSE)
  local r21_7 = CreateButton(r0_7, "btnLogin", 121, 23, 468, 334)
  SKY_Set(r0_7, PROP_FORM_ESC_BUTTON, CreateButton(r0_7, "btnExit", 121, 23, 468, 380))
  g_igw = SKY_Create(TYPE_UI_IGW, "igw")
  SKY_Set(g_igw, PROP_UI_POS, 0, 0)
  SKY_Set(g_igw, PROP_UI_SIZE, 10, 10)
  SKY_Set(r0_7, PROP_FORM_ADD, SKY_Create(TYPE_UI_CHECK_BOX, "saveAccount"))
  local r24_7 = SKY_Create(TYPE_UI_COMBO_BOX, "servercombox")
  SKY_Set(r0_7, PROP_FORM_ADD, r24_7)
  SKY_Set(r24_7, PROP_UI_POS, 834, 135)
  SKY_Set(r24_7, PROP_UI_SIZE, 111, 22)
  SKY_Set(r24_7, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r24_7, PROP_UI_COMBO_BOX_TEXT_POS, 24, 2)
  SKY_Set(r24_7, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r24_7, PROP_UI_FONT, DEFALUT_FONT3)
  login_combo_server_name = r24_7
  local r25_7 = SKY_Get(r24_7, PROP_UI_COMBO_BOX_MENU)
  login_combo_server_name = r25_7
  local r26_7 = SKY_Get(r25_7, PROP_UI_IMAGE)
  SKY_Set(r26_7, PROP_ITEM_RECT_STYLE, VAR_ITEM_RECT_FILL)
  SKY_Set(r26_7, PROP_UI_COLOR, 255, 0, 0, 0)
  local r27_7 = SKY_Get(r24_7, PROP_UI_COMBO_BOX_MENU)
  local r28_7 = SKY_Get(SKY_Get(r27_7, PROP_UI_MENU_ITEM), PROP_MENU_GROUP_SELECT_PIC)
  SKY_Set(r28_7, PROP_UI_COLOR, 255, 61, 61, 61)
  SKY_Set(r28_7, PROP_ITEM_RECT_COLOR, 255, 61, 61, 61)
  local r29_7 = SKY_Get(r24_7, PROP_UI_COMBO_BOX_BUTTON)
  SKY_Set(r29_7, PROP_UI_SIZE, 18, 19)
  SKY_Set(r29_7, PROP_UI_ALIGN, VAR_UI_ALIGN_LEFT_UP)
  local r30_7 = SKY_Get(r27_7, PROP_UI_MENU_ITEM)
  local r31_7 = SKY_Create(TYPE_UI_IMAGE, "imgFormImage")
  SKY_Set(r0_7, PROP_FORM_ADD, r31_7)
  SKY_Set(r31_7, PROP_UI_SIZE, 324, 300)
  SKY_Set(r31_7, PROP_UI_POS, 350, 325)
  SKY_Set(r31_7, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r31_7, PROP_UI_IMAGE), "login\\keyboard.dds", 324, 290, 0, 0)
  local r32_7 = SKY_Create(TYPE_UI_KEYBOARD, "keyboard")
  SKY_Set(r0_7, PROP_FORM_ADD, r32_7)
  SKY_Set(r32_7, PROP_UI_POS, 363, 416)
  SKY_Set(r32_7, PROP_UI_SIZE, 324, 260)
  SKY_Set(r32_7, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r32_7, PROP_UI_KBD_MAX_CHA, 20)
  SKY_Set(r32_7, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r32_7, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r32_7, PROP_UI_KBD_LOAD, "login\\keyboard.dds", 5, 10, 30, 27, 3, 3)
  keyboard = r32_7
  local r33_7 = SKY_Create(TYPE_UI_LABEL, "pasdlabel")
  SKY_Set(r0_7, PROP_FORM_ADD, r33_7)
  SKY_Set(r33_7, PROP_UI_POS, 403, 376)
  SKY_Set(r33_7, PROP_UI_SIZE, 200, 20)
  SKY_Set(r33_7, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r33_7, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r33_7, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r33_7, PROP_UI_SHOW, VAR_FALSE)
  local r34_7 = CreateButton(r0_7, "btncapitalization", 20, 20, 455, 471)
  SKY_Set(r34_7, PROP_UI_SHOW, VAR_TRUE)
  SKY_Set(r34_7, PROP_UI_HINT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1005))
  local r35_7 = CreateButton(r0_7, "btnaccount", 88, 28, 375, 588)
  SKY_Set(r35_7, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r35_7, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r35_7, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r35_7, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 25, 297)
  local r37_7 = CreateButton(r0_7, "btndelet", 60, 30, 545, 543)
  SKYImageLoad(SKY_Get(r37_7, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 60, 30, 241, 296)
  SKYImageLoad(SKY_Get(r37_7, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 60, 30, 241, 296)
  SKY_Set(r37_7, PROP_UI_SHOW, VAR_FALSE)
  local r38_7 = CreateButton(r0_7, "btnclose", 88, 28, 468, 588)
  SKY_Set(r38_7, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r38_7, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r38_7, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r38_7, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 118, 297)
  local r40_7 = SKY_Create(TYPE_UI_CHECK_BOX, "btnkeyboard")
  SKY_Set(r0_7, PROP_FORM_ADD, r40_7)
  SKY_Set(r40_7, PROP_UI_POS, 569, 595)
  SKY_Set(r40_7, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r40_7, PROP_UI_SIZE, 17, 17)
  SKY_Set(r40_7, PROP_UI_CHECK_ICON_SIZE, 17, 17)
  SKYImageLoad(SKY_Get(r40_7, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login\\keyboard.dds", 17, 17, 219, 302)
  SKYImageLoad(SKY_Get(r40_7, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login\\keyboard.dds", 17, 17, 219, 302)
end
function Create17173LoginForm()
  -- line: [945, 1327] id: 8
  local r0_8 = SKY_Create(TYPE_UI_FORM, "frmLogin")
  SKY_Set(r0_8, PROP_PROC, "login_form_proc")
  frmLogin = r0_8
  SKY_Set(r0_8, PROP_FORM_DESKTOP, dskLogin)
  SKY_Set(r0_8, PROP_UI_POS, 0, 0, 102, 16)
  SKY_Set(r0_8, PROP_UI_SIZE, 1024, 1024)
  local r1_8 = SKY_Create(TYPE_UI_IMAGE, "imgaccountblack")
  SKY_Set(r0_8, PROP_FORM_ADD, r1_8)
  SKY_Set(r1_8, PROP_UI_SIZE, 265, 310)
  SKY_Set(r1_8, PROP_UI_POS, 381, 122)
  SKYImageLoad(SKY_Get(r1_8, PROP_UI_IMAGE), "login/back_yuanshu17173.dds", 265, 310, 645, 487)
  local r2_8 = SKY_Create(TYPE_UI_IMAGE, "imgxuanqu")
  SKY_Set(r0_8, PROP_FORM_ADD, r2_8)
  SKY_Set(r2_8, PROP_UI_SIZE, 323, 485)
  SKY_Set(r2_8, PROP_UI_POS, 677, 112)
  SKYImageLoad(SKY_Get(r2_8, PROP_UI_IMAGE), "login/back_yuanshu17173.dds", 323, 490, 325, 0)
  local r3_8 = SKY_Create(TYPE_UI_IMAGE, "imgBoard")
  SKY_Set(r0_8, PROP_FORM_ADD, r3_8)
  SKY_Set(r3_8, PROP_UI_SIZE, 320, 490)
  SKY_Set(r3_8, PROP_UI_POS, 30, 113)
  SKYImageLoad(SKY_Get(r3_8, PROP_UI_IMAGE), "login/back_yuanshu17173.dds", 320, 490, 0, 0)
  local r4_8 = SKY_Create(TYPE_UI_LABEL, "versionlabel")
  SKY_Set(r0_8, PROP_FORM_ADD, r4_8)
  SKY_Set(r4_8, PROP_UI_POS, 410, 305)
  SKY_Set(r4_8, PROP_UI_SIZE, 115, 20)
  SKY_Set(r4_8, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r4_8, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r4_8, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1006) .. VAR_VERSION)
  local r5_8 = SKY_Create(TYPE_UI_LIST, "serverchang")
  SKY_Set(r0_8, PROP_FORM_ADD, r5_8)
  SKY_Set(r5_8, PROP_UI_POS, 710, 177)
  SKY_Set(r5_8, PROP_UI_SIZE, 279, 254)
  SKY_Set(r5_8, PROP_UI_COLOR, 255, 217, 214, 199)
  SKY_Set(SKY_Get(r5_8, PROP_UI_LIST_ITEM), PROP_ITEM_LIST_FONT, DEFALUT_FONT3)
  SKY_Set(r5_8, PROP_UI_MARGIN, 0, 35, 25, 0)
  local r6_8 = SKY_Create(TYPE_UI_SCROLL, "questtalkscroll")
  SKY_Set(r5_8, PROP_UI_LIST_SCROLL, r6_8)
  SKY_Set(r6_8, PROP_UI_POS, 203, 2)
  SKY_Set(r6_8, PROP_UI_SIZE, 22, 254)
  SKY_Set(r6_8, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r6_8, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r6_8, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r6_8, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r6_8, PROP_UI_SCROLL_VALUE, 1)
  local r7_8 = SKY_Create(TYPE_UI_IMAGE, "imgblack")
  SKY_Set(r6_8, PROP_UI_INSERT_CHILD, r7_8)
  SKY_Set(r7_8, PROP_UI_SIZE, 28, 106)
  SKY_Set(r7_8, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r7_8 = SKY_Get(r7_8, PROP_UI_IMAGE)
  local r8_8 = SKY_Get(r6_8, PROP_UI_SCROLL_UP)
  SKY_Set(r8_8, PROP_UI_SIZE, 22, 19)
  SKY_Set(r8_8, PROP_UI_POS, 0, 0)
  local r9_8 = SKY_Get(r6_8, PROP_UI_SCROLL_DOWN)
  SKY_Set(r9_8, PROP_UI_SIZE, 22, 19)
  SKY_Set(r9_8, PROP_UI_POS, 0, 235)
  local r10_8 = SKY_Get(r6_8, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r10_8, PROP_UI_SIZE, 17, 41)
  SKYImageLoad(SKY_Get(r10_8, PROP_UI_IMAGE), "login/back_yuanshu17173.dds", 17, 41, 0, 652)
  local r12_8 = SKY_Get(r5_8, PROP_UI_LIST_ITEM)
  SKY_Set(r12_8, PROP_UI_SIZE, 0, 1)
  SKY_Set(r12_8, PROP_ITEM_LIST_SELECT_OFFSET, 0, -5)
  SKY_Set(r12_8, PROP_ITEM_LIST_FOLLOW, VAR_FALSE)
  SKY_Set(r12_8, PROP_ITEM_LIST_UNIT_SPACE, -4)
  item_select = r12_8
  local r13_8 = SKY_Get(r12_8, PROP_ITEM_LIST_SELECT_PIC)
  SKY_Set(r13_8, PROP_UI_SIZE, 0, 1)
  SKY_Set(r13_8, PROP_UI_COLOR, 100, 255, 191, 0)
  SKY_Set(r13_8, PROP_IMAGE_LOAD, "system\\white.tga", 0, 1)
  local r14_8 = SKY_Create(TYPE_UI_MEMO, "systeminfo")
  SKY_Set(r0_8, PROP_FORM_ADD, r14_8)
  SKY_Set(r14_8, PROP_UI_POS, 45, 126)
  SKY_Set(r14_8, PROP_UI_SIZE, 275, 373)
  SKY_Set(r14_8, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_8, PROP_UI_MEMO_TEXT_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_8, PROP_UI_MEMO_TEXT_FONT, DEFALUT_FONT3)
  SKY_Set(r14_8, PROP_UI_MARGIN, 24, 25, 40, 25)
  SKY_Set(r14_8, PROP_UI_MEMO_LINE_SPACE, 1)
  SKY_Set(r14_8, PROP_UI_MEMO_TEXT_SHADE, VAR_FALSE)
  systeminfo = r14_8
  local r15_8 = SKY_Create(TYPE_UI_SCROLL, "systeminfoscroll")
  SKY_Set(r14_8, PROP_UI_MEMO_SCROLL, r15_8)
  SKY_Set(r15_8, PROP_UI_POS, 203, 2)
  SKY_Set(r15_8, PROP_UI_SIZE, 22, 373)
  SKY_Set(r15_8, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r15_8, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r15_8, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r15_8, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r15_8, PROP_UI_SCROLL_VALUE, 1)
  local r16_8 = SKY_Create(TYPE_UI_IMAGE, "systeminfo_imgblack")
  SKY_Set(r15_8, PROP_UI_INSERT_CHILD, r16_8)
  SKY_Set(r16_8, PROP_UI_SIZE, 28, 106)
  SKY_Set(r16_8, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r16_8 = SKY_Get(r16_8, PROP_UI_IMAGE)
  local r17_8 = SKY_Get(r15_8, PROP_UI_SCROLL_UP)
  SKY_Set(r17_8, PROP_UI_SIZE, 22, 15)
  SKY_Set(r17_8, PROP_UI_POS, 0, 0)
  local r18_8 = SKY_Get(r15_8, PROP_UI_SCROLL_DOWN)
  SKY_Set(r18_8, PROP_UI_SIZE, 22, 19)
  SKY_Set(r18_8, PROP_UI_POS, 0, 355)
  local r19_8 = SKY_Get(r15_8, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r19_8, PROP_UI_SIZE, 17, 40)
  SKYImageLoad(SKY_Get(r19_8, PROP_UI_IMAGE), "login/back_yuanshu17173.dds", 17, 40, 0, 652)
  edtAccount = CreateEdit(r0_8, "edtAccount", 177, 27, 456, 301)
  SKY_Set(edtAccount, PROP_UI_SHOW, VAR_FALSE)
  edtPassWord = CreateEdit(r0_8, "edtPassWord", 177, 27, 456, 332)
  SKY_Set(edtPassWord, PROP_UI_EDIT_PASSWORD, "*")
  SKY_Set(edtPassWord, PROP_UI_SHOW, VAR_FALSE)
  local r21_8 = CreateButton(r0_8, "btnLogin", 130, 23, 451, 330)
  LoadButton(r21_8, "login/back_yuanshu17173.dds", 130, 23, 419, 612, 1)
  LoadButton(r21_8, "login/back_yuanshu17173.dds", 130, 23, 419, 612, 2)
  LoadButton(r21_8, "login/back_yuanshu17173.dds", 130, 23, 419, 612, 3)
  local r22_8 = CreateButton(r0_8, "btnExit", 130, 23, 455, 378)
  LoadButton(r22_8, "login/back_yuanshu17173.dds", 130, 23, 423, 660, 1)
  LoadButton(r22_8, "login/back_yuanshu17173.dds", 130, 23, 423, 660, 2)
  LoadButton(r22_8, "login/back_yuanshu17173.dds", 130, 23, 423, 660, 3)
  SKY_Set(r0_8, PROP_FORM_ESC_BUTTON, r22_8)
  g_igw = SKY_Create(TYPE_UI_IGW, "igw")
  SKY_Set(g_igw, PROP_UI_POS, 0, 0)
  SKY_Set(g_igw, PROP_UI_SIZE, 10, 10)
  local r23_8 = SKY_Create(TYPE_UI_CHECK_BOX, "saveAccount")
  SKY_Set(r0_8, PROP_FORM_ADD, r23_8)
  SKY_Set(r23_8, PROP_UI_POS, 456, 420)
  SKY_Set(r23_8, PROP_UI_SIZE, 16, 17)
  SKY_Set(r23_8, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r23_8, PROP_UI_CHECK_ICON_SIZE, 16, 17)
  SKYImageLoad(SKY_Get(r23_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r23_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK_HOVER), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r23_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login/back_yuanshu17173.dds", 16, 17, 718, 395)
  SKYImageLoad(SKY_Get(r23_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login/back_yuanshu17173.dds", 16, 17, 718, 395)
  local r25_8 = SKY_Create(TYPE_UI_COMBO_BOX, "servercombox")
  SKY_Set(r0_8, PROP_FORM_ADD, r25_8)
  SKY_Set(r25_8, PROP_UI_POS, 834, 135)
  SKY_Set(r25_8, PROP_UI_SIZE, 111, 22)
  SKY_Set(r25_8, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r25_8, PROP_UI_COMBO_BOX_TEXT_POS, 24, 2)
  SKY_Set(r25_8, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r25_8, PROP_UI_FONT, DEFALUT_FONT3)
  login_combo_server_name = r25_8
  local r26_8 = SKY_Get(r25_8, PROP_UI_COMBO_BOX_MENU)
  login_combo_server_name = r26_8
  local r27_8 = SKY_Get(r26_8, PROP_UI_IMAGE)
  SKY_Set(r27_8, PROP_ITEM_RECT_STYLE, VAR_ITEM_RECT_FILL)
  SKY_Set(r27_8, PROP_UI_COLOR, 255, 0, 0, 0)
  local r28_8 = SKY_Get(r25_8, PROP_UI_COMBO_BOX_MENU)
  local r29_8 = SKY_Get(SKY_Get(r28_8, PROP_UI_MENU_ITEM), PROP_MENU_GROUP_SELECT_PIC)
  SKY_Set(r29_8, PROP_UI_COLOR, 255, 61, 61, 61)
  SKY_Set(r29_8, PROP_ITEM_RECT_COLOR, 255, 61, 61, 61)
  local r30_8 = SKY_Get(r25_8, PROP_UI_COMBO_BOX_BUTTON)
  SKY_Set(r30_8, PROP_UI_SIZE, 18, 19)
  SKY_Set(r30_8, PROP_UI_ALIGN, VAR_UI_ALIGN_LEFT_UP)
  local r31_8 = SKY_Get(r28_8, PROP_UI_MENU_ITEM)
  local r32_8 = SKY_Create(TYPE_UI_IMAGE, "imgFormImage")
  SKY_Set(r0_8, PROP_FORM_ADD, r32_8)
  SKY_Set(r32_8, PROP_UI_SIZE, 324, 290)
  SKY_Set(r32_8, PROP_UI_POS, 350, 325)
  SKY_Set(r32_8, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r32_8, PROP_UI_IMAGE), "login\\keyboard.dds", 324, 290, 0, 0)
  local r33_8 = SKY_Create(TYPE_UI_KEYBOARD, "keyboard")
  SKY_Set(r0_8, PROP_FORM_ADD, r33_8)
  SKY_Set(r33_8, PROP_UI_POS, 363, 408)
  SKY_Set(r33_8, PROP_UI_SIZE, 324, 260)
  SKY_Set(r33_8, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r33_8, PROP_UI_KBD_MAX_CHA, 20)
  SKY_Set(r33_8, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r33_8, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r33_8, PROP_UI_KBD_LOAD, "login\\keyboard.dds", 5, 10, 30, 27, 3, 3)
  keyboard = r33_8
  local r34_8 = SKY_Create(TYPE_UI_LABEL, "pasdlabel")
  SKY_Set(r0_8, PROP_FORM_ADD, r34_8)
  SKY_Set(r34_8, PROP_UI_POS, 403, 370)
  SKY_Set(r34_8, PROP_UI_SIZE, 200, 20)
  SKY_Set(r34_8, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r34_8, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r34_8, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r34_8, PROP_UI_SHOW, VAR_FALSE)
  local r35_8 = CreateButton(r0_8, "btncapitalization", 20, 20, 455, 461)
  SKY_Set(r35_8, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r35_8, PROP_UI_HINT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1007))
  local r36_8 = CreateButton(r0_8, "btnaccount", 88, 28, 375, 579)
  SKY_Set(r36_8, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r36_8, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r36_8, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r36_8, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 25, 297)
  local r38_8 = CreateButton(r0_8, "btndelet", 60, 30, 545, 536)
  SKYImageLoad(SKY_Get(r38_8, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 60, 30, 241, 296)
  SKYImageLoad(SKY_Get(r38_8, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 60, 30, 241, 296)
  SKY_Set(r38_8, PROP_UI_SHOW, VAR_FALSE)
  local r39_8 = CreateButton(r0_8, "btnclose", 88, 28, 468, 579)
  SKY_Set(r39_8, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r39_8, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r39_8, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r39_8, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 118, 297)
  local r41_8 = SKY_Create(TYPE_UI_CHECK_BOX, "btnkeyboard")
  SKY_Set(r0_8, PROP_FORM_ADD, r41_8)
  SKY_Set(r41_8, PROP_UI_POS, 569, 585)
  SKY_Set(r41_8, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r41_8, PROP_UI_SIZE, 17, 17)
  SKY_Set(r41_8, PROP_UI_CHECK_ICON_SIZE, 17, 17)
  SKYImageLoad(SKY_Get(r41_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login\\keyboard.dds", 17, 17, 219, 302)
  SKYImageLoad(SKY_Get(r41_8, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login\\keyboard.dds", 17, 17, 219, 302)
end
function Login17173Proc(r0_9)
  -- line: [1333, 1428] id: 9
  disconnet_tag = 1
  local r1_9 = 0
  local r2_9 = 0
  local r3_9 = -1
  local r4_9 = -1
  if SKY_Get(item_select, PROP_ITEM_LIST_SELECT) ~= -1 then
    local r6_9 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r3_9 = r6_9 / 10 - 1
    r1_9 = server_table[r6_9].ip_table[math.random(1, server_table[r6_9].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1008))
    return 
  end
  local r5_9 = SKY_Get(SKY_Get(r0_9, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1009))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r6_9 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_9, 3800)
  if r6_9 ~= 1 then
    if r6_9 == 2 then
      local r7_9 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_9, 3800)
      if r7_9 ~= 1 and r7_9 == 2 then
        local r8_9 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_9, 3800)
        if r8_9 ~= 1 and r8_9 == 2 then
          SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
          SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1010))
          SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
          return 
        end
      end
    elseif r6_9 == 3 then
      SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
      SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1011))
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
      return 
    end
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1012))
  local r7_9 = SKY_Set(VAR_APP, PROP_APP_173SEVER_LOGIN)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  if r7_9 == 1 then
    SKY_Set(VAR_APP, PROP_APP_CLEAR_GAME_DATA)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r7_9 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  end
end
function LoginSHUNProc(r0_10)
  -- line: [1435, 1532] id: 10
  disconnet_tag = 1
  local r1_10 = 0
  local r2_10 = 0
  local r3_10 = -1
  local r4_10 = -1
  if SKY_Get(item_select, PROP_ITEM_LIST_SELECT) ~= -1 then
    local r6_10 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r3_10 = r6_10 / 10 - 1
    r1_10 = server_table[r6_10].ip_table[math.random(1, server_table[r6_10].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1013))
    return 
  end
  local r5_10 = SKY_Get(SKY_Get(r0_10, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1014))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r6_10 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_10, 3800)
  if r6_10 ~= 1 then
    if r6_10 == 2 then
      local r7_10 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_10, 3800)
      if r7_10 ~= 1 and r7_10 == 2 then
        local r8_10 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_10, 3800)
        if r8_10 ~= 1 and r8_10 == 2 then
          SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
          SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1015))
          SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
          return 
        end
      end
    elseif r6_10 == 3 then
      SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
      SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1016))
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
      return 
    end
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1017))
  local r7_10 = SKY_Set(VAR_APP, PROP_APP_SHUNYOU_LOGIN)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  if r7_10 == 1 then
    SKY_Set(VAR_APP, PROP_APP_CLEAR_GAME_DATA)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r7_10 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  end
end
function LoginDefaultProc(r0_11)
  -- line: [1537, 1641] id: 11
  disconnet_tag = 1
  local r1_11 = SKY_Get(SKY_Get(r0_11, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  if string.len(r1_11) == 0 then
    SKY_Set(r0_11, PROP_FORM_ACTIVE, SKY_Get(r0_11, PROP_FORM_FIND, "edtAccount"))
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1018))
    return 
  end
  local r2_11 = FindFormUI(frmLogin, "saveAccount")
  local r3_11 = 0
  local r4_11 = 0
  local r5_11 = SKY_Get(r2_11, PROP_UI_CHECK_CHECK)
  local r6_11 = -1
  local r7_11 = -1
  r7_11 = SKY_Get(item_select, PROP_ITEM_LIST_SELECT)
  if r7_11 ~= -1 then
    local r9_11 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r6_11 = r9_11 / 10 - 1
    r3_11 = server_table[r9_11].ip_table[math.random(1, server_table[r9_11].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1019))
    return 
  end
  local r8_11 = SKY_Get(SKY_Get(r0_11, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1020))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r9_11 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_11, 3800)
  if r9_11 == 1 then
    SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_11, r8_11, r6_11, r7_11)
  elseif r9_11 == 2 then
    local r10_11 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_11, 3800)
    if r10_11 == 1 then
      SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_11, r8_11, r6_11, r7_11)
    elseif r10_11 == 2 then
      local r11_11 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_11, 3800)
      if r11_11 == 1 then
        SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_11, r8_11, r6_11, r7_11)
      elseif r11_11 == 2 then
        SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
        SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1021))
        SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
        return 
      end
    end
  elseif r9_11 == 3 then
    SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
    SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1022))
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
    return 
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1023))
  local r10_11 = SKY_Get(SKY_Get(r0_11, PROP_FORM_FIND, "edtPassWord"), PROP_UI_TEXT)
  local r11_11 = SKY_Set(VAR_APP, PROP_APP_LOGIN, 1, r1_11, r10_11)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  if r11_11 == 1 then
    SKY_Set(VAR_APP, PROP_APP_CLEAR_GAME_DATA)
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    SKY_Set(frmLogin, PROP_FORM_REMOVE, g_igw)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r11_11 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
    SKY_Set(edtPassWord, PROP_UI_TEXT, r10_11)
    SKY_Set(frmLogin, PROP_FORM_ACTIVE, edtAccount)
  end
end
function login_form_proc(r0_12, r1_12, r2_12)
  -- line: [1644, 1792] id: 12
  if r1_12 == MSG_FORM_SHOW then
    SKY_Set(item_select, PROP_ITEM_LIST_SELECT, 0)
    local r3_12 = SKY_Get(g_igw, PROP_UI_IGW_INIT)
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    SKY_Set(frmHotkey, PROP_FORM_REMOVE, g_igw)
    SKY_Set(frmLogin, PROP_FORM_ADD, g_igw)
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
  end
  if r1_12 == MSG_UI_EDIT_ENTER then
    local r3_12 = SKY_Get(r0_12, PROP_UI_FORM)
    if SKY_Get(r0_12, PROP_NAME) == "edtAccount" then
      if string.len(SKY_Get(r0_12, PROP_UI_TEXT)) > 0 then
        SKY_Set(r3_12, PROP_FORM_ACTIVE, SKY_Get(r3_12, PROP_FORM_FIND, "edtPassWord"))
      end
      return 
    end
    if SKY_Get(r0_12, PROP_NAME) == "edtPassWord" then
      SKY_Set(SKY_Get(r3_12, PROP_FORM_FIND, "btnLogin"), PROP_UI_BUTTON_CLICK)
      return 
    end
  end
  if r1_12 == MSG_UI_COMBO_BOX_CHANGE then
    local r4_12 = SKYParamPop(r2_12) + 1
    local r5_12 = server_table[r4_12]
    SKY_Set(SKY_Get(FindFormUI(frmLogin, "serverchang"), PROP_UI_LIST_ITEM), PROP_ITEM_LIST_CLEAR)
    for r11_12 = 1, r5_12, 1 do
      server_list(server_table[tonumber(r4_12 .. r11_12)].ip_name, tonumber(r4_12 .. r11_12))
    end
  end
  if r1_12 == MSG_UI_CHECK_CHANGE then
    local r3_12 = SKYParamPop(r2_12)
    if SKY_Get(r0_12, PROP_NAME) == "btnkeyboard" then
      SKY_Set(keyboard, PROP_UI_KBD_CAPS_LOCK, r3_12)
    end
  end
  if r1_12 == MSG_UI_BUTTON_CLICK then
    if SKY_Get(r0_12, PROP_NAME) == "btnLogin" then
      local r3_12 = SKY_Get(r0_12, PROP_UI_FORM)
      if g_GAME_VISION == VAR_APP_GAME_DEFAULT then
        LoginDefaultProc(r3_12)
      elseif g_GAME_VISION == VAR_APP_GAME_SHUN then
        LoginSHUNProc(r3_12)
      elseif g_GAME_VISION == VAR_APP_GAME_17173 then
        Login17173Proc(r3_12)
      elseif g_GAME_VISION == VAR_APP_GAME_MOQI then
        LoginMoQiProc(r3_12)
      end
    end
    if SKY_Get(r0_12, PROP_NAME) == "btnExit" then
      SKY_Set(VAR_TIME, PROP_TIME_IS_RUN, VAR_FALSE)
      return 
    end
    if SKY_Get(r0_12, PROP_NAME) == "btncapitalization" then
      safety_bool(VAR_TRUE, VAR_FALSE)
    elseif SKY_Get(r0_12, PROP_NAME) == "btnaccount" then
      keyboard_LoginProc(SKY_Get(r0_12, PROP_UI_FORM))
      return 
    elseif SKY_Get(r0_12, PROP_NAME) == "btndelet" then
      pasd_label_num(-1)
      SKY_Set(keyboard, PROP_UI_KBD_DEL)
    elseif SKY_Get(r0_12, PROP_NAME) == "btnclose" then
      SKY_Set(FindFormUI(frmLogin, "pasdlabel"), PROP_UI_TEXT, "")
      SKY_Set(keyboard, PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(keyboard, PROP_UI_KBD_CLEAR)
      safety_bool(VAR_FALSE, VAR_TRUE)
    end
    if SKY_Get(r0_12, PROP_NAME) == "btnDragon" then
      SKY_Set(FindFormUI(frmLogin, "edtAccount"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "edtPassWord"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "btnLogin"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "btnExit"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "imgaccountblack"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "saveAccount"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(FindFormUI(frmLogin, "btncapitalization"), PROP_UI_SHOW, VAR_TRUE)
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
      if SKY_Get(keyboard, PROP_UI_SHOW) == VAR_TRUE then
        safety_bool(VAR_FALSE, VAR_TRUE)
      end
      SKYImageLoad(SKY_Get(FindFormUI(frmLogin, "Dragon_btn"), PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 416, 572)
      SKYImageLoad(SKY_Get(FindFormUI(frmLogin, "legend_btn"), PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 516, 549)
      SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    elseif SKY_Get(r0_12, PROP_NAME) == "btnlegend" then
      SKY_Set(FindFormUI(frmLogin, "imgaccountblack"), PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 2)
      SKY_Set(g_igw, PROP_UI_IGW_MOVE_ENABLE, VAR_FALSE)
      SKY_Set(FindFormUI(frmLogin, "edtAccount"), PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(FindFormUI(frmLogin, "edtPassWord"), PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(FindFormUI(frmLogin, "btnLogin"), PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(FindFormUI(frmLogin, "btnExit"), PROP_UI_SHOW, VAR_FALSE)
      local r8_12 = FindFormUI(frmLogin, "saveAccount")
      SKY_Set(FindFormUI(frmLogin, "btncapitalization"), PROP_UI_SHOW, VAR_FALSE)
      SKY_Set(r8_12, PROP_UI_SHOW, VAR_FALSE)
      if SKY_Get(keyboard, PROP_UI_SHOW) == VAR_TRUE then
        safety_bool(VAR_FALSE, VAR_FALSE)
      end
      SKYImageLoad(SKY_Get(FindFormUI(frmLogin, "Dragon_btn"), PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 416, 549)
      SKYImageLoad(SKY_Get(FindFormUI(frmLogin, "legend_btn"), PROP_UI_IMAGE), "login/back_yuanshu.dds", 98, 18, 516, 572)
    end
  end
end
function CreateSelectFormBack()
  -- line: [1793, 1815] id: 13
  local r0_13 = SKY_Create(TYPE_UI_FORM, "frmSelect_back")
  SKY_Set(r0_13, PROP_PROC, "select_back_form_proc")
  SKY_Set(r0_13, PROP_FORM_DESKTOP, dskSelect)
  SKY_Set(r0_13, PROP_UI_POS, 0, 0)
  SKY_Set(r0_13, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  SKY_Set(r0_13, PROP_UI_ENABLED, VAR_FALSE)
  selectformback = r0_13
  local r1_13 = SKY_Create(TYPE_UI_IMAGE, "imgSelect")
  SKY_Set(r0_13, PROP_FORM_ADD, r1_13)
  SKY_Set(r1_13, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  SKYImageLoad(SKY_Get(r1_13, PROP_UI_IMAGE), "select\\back1.dds", 1024, 768, 0, 0)
end
CreateSelectFormBack()
function select_back_form_proc()
  -- line: [1817, 1818] id: 14
end
function CreateSelectForm()
  -- line: [1820, 2227] id: 15
  local r0_15 = SKY_Create(TYPE_UI_FORM, "frmSelect")
  SKY_Set(r0_15, PROP_PROC, "select_form_proc")
  SKY_Set(r0_15, PROP_FORM_DESKTOP, dskSelect)
  SKY_Set(r0_15, PROP_UI_POS, 0, 0, 102, 16)
  SKY_Set(r0_15, PROP_UI_SIZE, 1024, 1024)
  frmSelect = r0_15
  local r1_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1_back1")
  SKY_Set(r0_15, PROP_FORM_ADD, r1_15)
  SKY_Set(r1_15, PROP_UI_POS, 42, 108)
  SKY_Set(r1_15, PROP_UI_SIZE, 262, 64)
  SKYImageLoad(SKY_Get(r1_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 0)
  local r2_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1")
  SKY_Set(r0_15, PROP_FORM_ADD, r2_15)
  SKY_Set(r2_15, PROP_UI_POS, 42, 108)
  SKY_Set(r2_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r2_15, PROP_UI_SHOW, VAR_FALSE)
  Select.Circle[1] = r2_15
  local r3_15 = SKY_Create(TYPE_UI_SHOT, "shtImage1")
  SKY_Set(r0_15, PROP_FORM_ADD, r3_15)
  SKY_Set(r3_15, PROP_UI_POS, 42, 108)
  SKY_Set(r3_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r3_15, PROP_UI_TAG, 1)
  Select.Shot[1] = r3_15
  local r4_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1_back2")
  SKY_Set(r0_15, PROP_FORM_ADD, r4_15)
  SKY_Set(r4_15, PROP_UI_POS, 42, 171)
  SKY_Set(r4_15, PROP_UI_SIZE, 262, 64)
  SKYImageLoad(SKY_Get(r4_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 66)
  local r5_15 = SKY_Create(TYPE_UI_IMAGE, "Circle2")
  SKY_Set(r0_15, PROP_FORM_ADD, r5_15)
  SKY_Set(r5_15, PROP_UI_POS, 42, 171)
  SKY_Set(r5_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r5_15, PROP_UI_SHOW, VAR_FALSE)
  Select.Circle[2] = r5_15
  SKYImageLoad(SKY_Get(r5_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 66)
  local r6_15 = SKY_Create(TYPE_UI_SHOT, "shtImage2")
  SKY_Set(r0_15, PROP_FORM_ADD, r6_15)
  SKY_Set(r6_15, PROP_UI_POS, 42, 171)
  SKY_Set(r6_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r6_15, PROP_UI_TAG, 2)
  Select.Shot[2] = r6_15
  local r7_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1_back3")
  SKY_Set(r0_15, PROP_FORM_ADD, r7_15)
  SKY_Set(r7_15, PROP_UI_POS, 42, 234)
  SKY_Set(r7_15, PROP_UI_SIZE, 262, 64)
  SKYImageLoad(SKY_Get(r7_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 129)
  local r8_15 = SKY_Create(TYPE_UI_IMAGE, "Circle3")
  SKY_Set(r0_15, PROP_FORM_ADD, r8_15)
  SKY_Set(r8_15, PROP_UI_POS, 42, 234)
  SKY_Set(r8_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r8_15, PROP_UI_SHOW, VAR_FALSE)
  Select.Circle[3] = r8_15
  SKYImageLoad(SKY_Get(r8_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 129)
  local r9_15 = SKY_Create(TYPE_UI_SHOT, "shtImage3")
  SKY_Set(r0_15, PROP_FORM_ADD, r9_15)
  SKY_Set(r9_15, PROP_UI_POS, 42, 234)
  SKY_Set(r9_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r9_15, PROP_UI_TAG, 3)
  Select.Shot[3] = r9_15
  local r10_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1_back4")
  SKY_Set(r0_15, PROP_FORM_ADD, r10_15)
  SKY_Set(r10_15, PROP_UI_POS, 42, 298)
  SKY_Set(r10_15, PROP_UI_SIZE, 262, 64)
  SKYImageLoad(SKY_Get(r10_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 194)
  local r11_15 = SKY_Create(TYPE_UI_IMAGE, "Circle4")
  SKY_Set(r0_15, PROP_FORM_ADD, r11_15)
  SKY_Set(r11_15, PROP_UI_POS, 42, 298)
  SKY_Set(r11_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r11_15, PROP_UI_SHOW, VAR_FALSE)
  Select.Circle[4] = r11_15
  SKYImageLoad(SKY_Get(r11_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 194)
  local r12_15 = SKY_Create(TYPE_UI_SHOT, "shtImage4")
  SKY_Set(r0_15, PROP_FORM_ADD, r12_15)
  SKY_Set(r12_15, PROP_UI_POS, 42, 298)
  SKY_Set(r12_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r12_15, PROP_UI_TAG, 4)
  Select.Shot[4] = r12_15
  local r13_15 = SKY_Create(TYPE_UI_IMAGE, "Circle1_back5")
  SKY_Set(r0_15, PROP_FORM_ADD, r13_15)
  SKY_Set(r13_15, PROP_UI_POS, 42, 363)
  SKY_Set(r13_15, PROP_UI_SIZE, 262, 64)
  SKYImageLoad(SKY_Get(r13_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 257)
  local r14_15 = SKY_Create(TYPE_UI_IMAGE, "Circle5")
  SKY_Set(r0_15, PROP_FORM_ADD, r14_15)
  SKY_Set(r14_15, PROP_UI_POS, 42, 363)
  SKY_Set(r14_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r14_15, PROP_UI_SHOW, VAR_FALSE)
  Select.Circle[5] = r14_15
  SKYImageLoad(SKY_Get(r14_15, PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, 0, 257)
  local r15_15 = SKY_Create(TYPE_UI_SHOT, "shtImage5")
  SKY_Set(r0_15, PROP_FORM_ADD, r15_15)
  SKY_Set(r15_15, PROP_UI_POS, 42, 363)
  SKY_Set(r15_15, PROP_UI_SIZE, 262, 64)
  SKY_Set(r15_15, PROP_UI_TAG, 5)
  Select.Shot[5] = r15_15
  local r16_15 = SKY_Create(TYPE_UI_BUTTON, "btnOK")
  SKY_Set(r0_15, PROP_FORM_ADD, r16_15)
  SKY_Set(r0_15, PROP_FORM_ENTER_BUTTON, r16_15)
  SKY_Set(r16_15, PROP_UI_BUTTON_CLICK_SOUND, 22)
  SKY_Set(r16_15, PROP_UI_POS, 303, 169)
  SKY_Set(r16_15, PROP_UI_SIZE, 129, 63)
  select_play_ok_button.button = r16_15
  SKYImageLoad(SKY_Get(r16_15, PROP_UI_IMAGE, 0), "select\\selectyuanshu.dds", 129, 63, 0, 452)
  SKYImageLoad(SKY_Get(r16_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 129, 63, 0, 512)
  SKYImageLoad(SKY_Get(r16_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 129, 63, 0, 512)
  SKYImageLoad(SKY_Get(r16_15, PROP_UI_IMAGE, 3), "select\\selectyuanshu.dds", 129, 63, 0, 512)
  local r18_15 = SKY_Create(TYPE_UI_IMAGE, "imgSelect")
  SKY_Set(r0_15, PROP_FORM_ADD, r18_15)
  SKY_Set(r18_15, PROP_UI_POS, 24, 50)
  SKY_Set(r18_15, PROP_UI_SIZE, 292, 693)
  r18_15 = SKY_Get(r18_15, PROP_UI_IMAGE)
  if g_GAME_VISION == VAR_APP_GAME_SHUN or g_GAME_VISION == VAR_APP_GAME_17173 then
    SKYImageLoad(r18_15, "select\\selectyuanshu17173andshun.dds", 292, 693, 730, 330)
  else
    SKYImageLoad(r18_15, "select\\selectyuanshu.dds", 292, 693, 730, 330)
  end
  local r19_15 = SKY_Create(TYPE_UI_IMAGE, "ImgSign")
  SKY_Set(r0_15, PROP_FORM_ADD, r19_15)
  SKY_Set(r19_15, PROP_UI_POS, 144, 52)
  SKY_Set(r19_15, PROP_UI_SIZE, 56, 69)
  Select.Sign = r19_15
  local r20_15 = SKY_Create(TYPE_UI_BUTTON, "btnCreate")
  SKY_Set(r0_15, PROP_FORM_ADD, r20_15)
  SKY_Set(r20_15, PROP_UI_BUTTON_CLICK_SOUND, 23)
  SKY_Set(r20_15, PROP_UI_POS, 106, 442)
  SKY_Set(r20_15, PROP_UI_SIZE, 135, 33)
  SKYImageLoad(SKY_Get(r20_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 135, 33, 5, 330)
  SKYImageLoad(SKY_Get(r20_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 135, 33, 5, 330)
  local r22_15 = SKY_Create(TYPE_UI_BUTTON, "btnDelete")
  SKY_Set(r0_15, PROP_FORM_ADD, r22_15)
  SKY_Set(r22_15, PROP_UI_BUTTON_CLICK_SOUND, 24)
  SKY_Set(r22_15, PROP_UI_POS, 106, 493)
  SKY_Set(r22_15, PROP_UI_SIZE, 135, 33)
  SKYImageLoad(SKY_Get(r22_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 135, 33, 5, 363)
  SKYImageLoad(SKY_Get(r22_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 135, 33, 5, 363)
  local r25_15 = SKY_Create(TYPE_UI_BUTTON, "btnReturn")
  SKY_Set(r0_15, PROP_FORM_ADD, r25_15)
  SKY_Set(r0_15, PROP_FORM_ESC_BUTTON, r25_15)
  SKY_Set(r25_15, PROP_UI_BUTTON_CLICK_SOUND, 22)
  SKY_Set(r25_15, PROP_UI_POS, 106, 546)
  SKY_Set(r25_15, PROP_UI_SIZE, 134, 33)
  if g_GAME_VISION == VAR_APP_GAME_SHUN or g_GAME_VISION == VAR_APP_GAME_17173 then
    SKYImageLoad(SKY_Get(r25_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu17173andshun.dds", 134, 33, 5, 396)
    SKYImageLoad(SKY_Get(r25_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu17173andshun.dds", 134, 33, 5, 396)
  else
    SKYImageLoad(SKY_Get(r25_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 134, 33, 5, 396)
    SKYImageLoad(SKY_Get(r25_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 134, 33, 5, 396)
  end
  local r26_15 = SKY_Create(TYPE_UI_LABEL, "namelabel1")
  SKY_Set(r0_15, PROP_FORM_ADD, r26_15)
  SKY_Set(r26_15, PROP_UI_POS, 67, 120)
  SKY_Set(r26_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r26_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r26_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r26_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r26_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1024))
  chaname[1] = r26_15
  local r27_15 = SKY_Create(TYPE_UI_LABEL, "namelabel2")
  SKY_Set(r0_15, PROP_FORM_ADD, r27_15)
  SKY_Set(r27_15, PROP_UI_POS, 67, 182)
  SKY_Set(r27_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r27_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r27_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r27_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r27_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1025))
  chaname[2] = r27_15
  local r28_15 = SKY_Create(TYPE_UI_LABEL, "namelabel3")
  SKY_Set(r0_15, PROP_FORM_ADD, r28_15)
  SKY_Set(r28_15, PROP_UI_POS, 67, 246)
  SKY_Set(r28_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r28_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r28_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r28_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r28_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1026))
  chaname[3] = r28_15
  local r29_15 = SKY_Create(TYPE_UI_LABEL, "namelabel4")
  SKY_Set(r0_15, PROP_FORM_ADD, r29_15)
  SKY_Set(r29_15, PROP_UI_POS, 67, 309)
  SKY_Set(r29_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r29_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r29_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r29_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r29_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1027))
  chaname[4] = r29_15
  local r30_15 = SKY_Create(TYPE_UI_LABEL, "namelabel5")
  SKY_Set(r0_15, PROP_FORM_ADD, r30_15)
  SKY_Set(r30_15, PROP_UI_POS, 67, 375)
  SKY_Set(r30_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r30_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r30_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r30_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r30_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1028))
  chaname[5] = r30_15
  local r31_15 = SKY_Create(TYPE_UI_LABEL, "joblabel1")
  SKY_Set(r0_15, PROP_FORM_ADD, r31_15)
  SKY_Set(r31_15, PROP_UI_POS, 70, 139)
  SKY_Set(r31_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r31_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r31_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r31_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r31_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1029))
  job_level[1] = r31_15
  local r32_15 = SKY_Create(TYPE_UI_LABEL, "joblabel2")
  SKY_Set(r0_15, PROP_FORM_ADD, r32_15)
  SKY_Set(r32_15, PROP_UI_POS, 70, 201)
  SKY_Set(r32_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r32_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r32_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r32_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r32_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1030))
  job_level[2] = r32_15
  local r33_15 = SKY_Create(TYPE_UI_LABEL, "joblabel3")
  SKY_Set(r0_15, PROP_FORM_ADD, r33_15)
  SKY_Set(r33_15, PROP_UI_POS, 70, 267)
  SKY_Set(r33_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r33_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r33_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r33_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r33_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1031))
  job_level[3] = r33_15
  local r34_15 = SKY_Create(TYPE_UI_LABEL, "joblabel4")
  SKY_Set(r0_15, PROP_FORM_ADD, r34_15)
  SKY_Set(r34_15, PROP_UI_POS, 70, 330)
  SKY_Set(r34_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r34_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r34_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r34_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r34_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1032))
  job_level[4] = r34_15
  local r35_15 = SKY_Create(TYPE_UI_LABEL, "joblabel5")
  SKY_Set(r0_15, PROP_FORM_ADD, r35_15)
  SKY_Set(r35_15, PROP_UI_POS, 70, 392)
  SKY_Set(r35_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r35_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r35_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r35_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r35_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1033))
  job_level[5] = r35_15
  local r36_15 = SKY_Create(TYPE_UI_LABEL, "zonelabel")
  SKY_Set(r0_15, PROP_FORM_ADD, r36_15)
  SKY_Set(r36_15, PROP_UI_POS, 60, 373)
  SKY_Set(r36_15, PROP_UI_SIZE, 120, 16)
  SKY_Set(r36_15, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r36_15, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r36_15, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r36_15, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r36_15, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1034))
  zone = r36_15
  local r37_15 = SKY_Create(TYPE_UI_BUTTON, "btnLeft")
  SKY_Set(r0_15, PROP_FORM_ADD, r37_15)
  SKY_Set(r37_15, PROP_UI_POS, 556, 696)
  SKY_Set(r37_15, PROP_UI_SIZE, 45, 45)
  SKYImageLoad(SKY_Get(r37_15, PROP_UI_IMAGE, 0), "select\\selectyuanshu.dds", 45, 45, 441, 402)
  SKYImageLoad(SKY_Get(r37_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 40, 41, 439, 362)
  SKYImageLoad(SKY_Get(r37_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 45, 45, 441, 402)
  SKYImageLoad(SKY_Get(r37_15, PROP_UI_IMAGE, 3), "select\\selectyuanshu.dds", 40, 41, 439, 362)
  local r40_15 = SKY_Create(TYPE_UI_BUTTON, "btnRight")
  SKY_Set(r0_15, PROP_FORM_ADD, r40_15)
  SKY_Set(r40_15, PROP_UI_POS, 683, 696)
  SKY_Set(r40_15, PROP_UI_SIZE, 45, 45)
  SKYImageLoad(SKY_Get(r40_15, PROP_UI_IMAGE, 0), "select\\selectyuanshu.dds", 45, 45, 389, 402)
  SKYImageLoad(SKY_Get(r40_15, PROP_UI_IMAGE, 1), "select\\selectyuanshu.dds", 40, 41, 398, 362)
  SKYImageLoad(SKY_Get(r40_15, PROP_UI_IMAGE, 2), "select\\selectyuanshu.dds", 45, 45, 389, 402)
  SKYImageLoad(SKY_Get(r40_15, PROP_UI_IMAGE, 3), "select\\selectyuanshu.dds", 40, 41, 398, 362)
end
select_play_job = {
  [2] = {
    x_no = 262,
    y_no = 194,
    x_yes = 523,
    y_yes = 194,
  },
  [3] = {
    x_no = 262,
    y_no = 66,
    x_yes = 523,
    y_yes = 66,
  },
  [4] = {
    x_no = 262,
    y_no = 129,
    x_yes = 523,
    y_yes = 129,
  },
  [5] = {
    x_no = 262,
    y_no = 257,
    x_yes = 523,
    y_yes = 257,
  },
  [6] = {
    x_no = 262,
    y_no = 0,
    x_yes = 523,
    y_yes = 0,
  },
}
function select_form_proc(r0_16, r1_16, r2_16)
  -- line: [2238, 2283] id: 16
  PrintMsg("select", r0_16, r1_16, r2_16)
  if r1_16 == MSG_UI_BUTTON_CLICK then
    if SKY_Get(r0_16, PROP_NAME) == "btnReturn" then
      if g_GAME_VISION == VAR_APP_GAME_SHUN or g_GAME_VISION == VAR_APP_GAME_17173 then
        SKY_Set(VAR_TIME, PROP_TIME_IS_RUN, VAR_FALSE)
        return 
      end
      SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskLogin)
      SKY_Get(VAR_APP, PROP_APP_ACCOUNT_SAVE)
      disconnet_tag = 1
    end
    if SKY_Get(r0_16, PROP_NAME) == "btnOK" then
    end
    if SKY_Get(r0_16, PROP_NAME) == "btnCreate" then
      SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskCreate)
    end
    return 
  elseif r1_16 == MSG_FORM_SHOW then
    for r6_16 = 1, 5, 1 do
      if SKY_Get(chaname[r6_16], PROP_UI_TEXT) == "" then
        SKY_Set(chaname[r6_16], PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1035))
        SKY_Set(job_level[r6_16], PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1036))
      end
    end
    local r3_16 = SKY_Get(Select.Sign, PROP_UI_IMAGE)
    SKY_Set(r3_16, PROP_UI_SHOW, VAR_FALSE)
    SKYImageLoad(r3_16, "select\\selectyuanshu.dds", 56, 69, 1023, 1023)
  elseif r1_16 == MSG_UI_SHOT_LDOWN then
    local r3_16 = SKY_Get(r0_16, PROP_UI_TAG)
    if r3_16 <= SKY_Get(VAR_FORM_SELECT, PROP_SELECT_COUNT) then
      SKY_Set(VAR_FORM_SELECT, PROP_SELECT_SELECT, r3_16)
    end
  end
end
SKY_Set(VAR_FORM_SELECT, PROP_PROC, "var_select_proc")
function var_select_proc(r0_17, r1_17, r2_17)
  -- line: [2287, 2342] id: 17
  PrintMsg("select", r0_17, r1_17, r2_17)
  if r1_17 == MSG_SELECT_CLEAR then
    Select:clear()
    local r3_17 = SKY_Get(Select.Sign, PROP_UI_IMAGE)
    SKY_Set(r3_17, PROP_UI_SHOW, VAR_FALSE)
    SKYImageLoad(r3_17, "select\\selectyuanshu.dds", 56, 69, 1023, 1023)
  elseif r1_17 == MSG_SELECT_ADD then
    local r3_17, r4_17 = SKYParamParse(r2_17)
    SKY_Set(Select.Circle[r3_17], PROP_UI_SHOW, VAR_TRUE)
    local r5_17 = SKY_Get(r4_17, PROP_SELECT_CHA_TYPE_ID)
    Select.TypeId[r3_17] = {}
    Select.TypeId[r3_17].type_id = SKY_Get(VAR_FORM_SELECT, PROP_CHA_CAMP, r3_17)
    Select.TypeId[r3_17].chajob_id = select_play(r5_17)
    SKYImageLoad(SKY_Get(Select.Circle[r3_17], PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, select_play_job[Select.TypeId[r3_17].chajob_id].x_no, select_play_job[Select.TypeId[r3_17].chajob_id].y_no)
    Select.count = Select.count + 1
  elseif r1_17 == MSG_SELECT_CHANGE then
    local r3_17 = SKYParamParse(r2_17) + 1
    local r4_17 = nil
    local r5_17 = nil
    local r6_17 = nil
    for r10_17 = 1, Select.count, 1 do
      if r10_17 == r3_17 then
        local r11_17 = Select.TypeId[r10_17].type_id
        if r11_17 == 1 then
          local r12_17 = SKY_Get(Select.Sign, PROP_UI_IMAGE)
          SKY_Set(r12_17, PROP_UI_SHOW, VAR_TRUE)
          SKYImageLoad(r12_17, "select\\selectyuanshu.dds", 56, 69, 250, 361)
        elseif r11_17 == 2 then
          local r12_17 = SKY_Get(Select.Sign, PROP_UI_IMAGE)
          SKY_Set(r12_17, PROP_UI_SHOW, VAR_TRUE)
          SKYImageLoad(r12_17, "select\\selectyuanshu.dds", 56, 69, 306, 361)
        else
          local r12_17 = SKY_Get(Select.Sign, PROP_UI_IMAGE)
          SKY_Set(r12_17, PROP_UI_SHOW, VAR_FALSE)
          SKYImageLoad(r12_17, "select\\selectyuanshu.dds", 56, 69, 486, 476)
        end
        SKYImageLoad(SKY_Get(Select.Circle[r3_17], PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, select_play_job[Select.TypeId[r3_17].chajob_id].x_yes, select_play_job[Select.TypeId[r3_17].chajob_id].y_yes)
        SKY_Set(select_play_ok_button.button, PROP_UI_POS, select_play_ok_button[r10_17].x, select_play_ok_button[r10_17].y)
      else
        SKYImageLoad(SKY_Get(Select.Circle[r10_17], PROP_UI_IMAGE), "select\\selectyuanshu.dds", 262, 64, select_play_job[Select.TypeId[r10_17].chajob_id].x_no, select_play_job[Select.TypeId[r10_17].chajob_id].y_no)
      end
      SKY_Set(Select.Circle[r10_17], PROP_UI_REFRESH)
      SKY_Set(frmSelect, PROP_UI_REFRESH)
    end
  end
end
function select_play(r0_18)
  -- line: [2344, 2347] id: 18
  local r1_18 = tonumber(SKY_Get(VAR_TABLE_CHA, PROP_TABLE_DATA, r0_18, "Job"))
  return r1_18, select_play_job[r1_18].x_no, select_play_job[r1_18].y_no, select_play_job[r1_18].x_yes, select_play_job[r1_18].y_yes
end
function server_list(r0_19, r1_19)
  -- line: [2350, 2363] id: 19
  local r3_19 = SKY_Get(FindFormUI(frmLogin, "serverchang"), PROP_UI_LIST_ITEM)
  local r4_19 = SKY_Create(TYPE_ITEM_MANY, "", 1)
  local r5_19 = SKY_Create(TYPE_TEXT)
  SKY_Set(r5_19, PROP_TEXT_FONT, DEFALUT_FONT3)
  SKY_Set(r5_19, PROP_UI_COLOR, COLOR_GOLDEN)
  SKY_Set(r5_19, PROP_TEXT_STRING, r0_19)
  SKY_Set(r4_19, PROP_ITEM_MANY_ITEM, 0, r5_19, 20, 0)
  SKY_Set(r4_19, PROP_UI_TAG, r1_19)
  SKY_Set(r3_19, PROP_ITEM_LIST_ADD, r4_19)
  SKY_Set(frmLogin, PROP_UI_REFRESH)
end
function music_page(r0_20)
  -- line: [2365, 2369] id: 20
  SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC_STOP)
  SKY_Set(VAR_SOUND, PROP_SOUND_MUSIC, "music/mp3/" .. r0_20 .. ".mp3")
end
function keyboard_LoginProc(r0_21)
  -- line: [2371, 2447] id: 21
  disconnet_tag = 1
  local r1_21 = SKY_Get(SKY_Get(r0_21, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  if string.len(r1_21) == 0 then
    SKY_Set(r0_21, PROP_FORM_ACTIVE, SKY_Get(r0_21, PROP_FORM_FIND, "edtAccount"))
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1037))
    return 
  end
  local r2_21 = FindFormUI(frmLogin, "saveAccount")
  local r3_21 = 0
  local r4_21 = 0
  local r5_21 = SKY_Get(r2_21, PROP_UI_CHECK_CHECK)
  local r6_21 = -1
  local r7_21 = -1
  if SKY_Get(item_select, PROP_ITEM_LIST_SELECT) ~= -1 then
    local r9_21 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r6_21 = r9_21 / 10 - 1
    r3_21 = server_table[r9_21].ip_table[math.random(1, server_table[r9_21].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1038))
    return 
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1039))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r8_21 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_21, 3800)
  if r8_21 ~= 1 then
    if r8_21 == 2 then
      local r9_21 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_21, 3800)
      if r9_21 ~= 1 and r9_21 == 2 then
        local r10_21 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_21, 3800)
        if r10_21 ~= 1 and r10_21 == 2 then
          SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
          SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1040))
          SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
          return 
        end
      end
    elseif r8_21 == 3 then
      SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
      SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1041))
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
      return 
    end
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1042))
  local r9_21 = SKY_Set(VAR_APP, PROP_APP_LOGIN, 2, r1_21)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  SKY_Set(FindFormUI(frmLogin, "pasdlabel"), PROP_UI_TEXT, "")
  SKY_Set(keyboard, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(keyboard, PROP_UI_KBD_CLEAR)
  safety_bool(VAR_FALSE, VAR_TRUE)
  if r9_21 == 1 then
    SKY_Set(VAR_APP, PROP_APP_CLEAR_GAME_DATA)
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    SKY_Set(frmLogin, PROP_FORM_REMOVE, g_igw)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r9_21 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  end
end
function pasd_label_num(r0_22)
  -- line: [2449, 2458] id: 22
  local r1_22 = FindFormUI(frmLogin, "pasdlabel")
  local r3_22 = ""
  for r7_22 = 1, string.len(SKY_Get(r1_22, PROP_UI_TEXT)) + r0_22, 1 do
    r3_22 = r3_22 .. "*"
  end
  SKY_Set(r1_22, PROP_UI_TEXT, r3_22)
end
function safety_bool(r0_23, r1_23)
  -- line: [2461, 2492] id: 23
  SKY_Set(FindFormUI(frmLogin, "imgFormImage"), PROP_UI_SHOW, r0_23)
  SKY_Set(keyboard, PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "btnaccount"), PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "btndelet"), PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "btnclose"), PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "pasdlabel"), PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "btnkeyboard"), PROP_UI_SHOW, r0_23)
  SKY_Set(FindFormUI(frmLogin, "btncapitalization"), PROP_UI_SHOW, r1_23)
  SKY_Set(FindFormUI(frmLogin, "saveAccount"), PROP_UI_SHOW, r1_23)
  SKY_Set(FindFormUI(frmLogin, "edtPassWord"), PROP_UI_SHOW, r1_23)
  SKY_Set(FindFormUI(frmLogin, "btnLogin"), PROP_UI_SHOW, r1_23)
  SKY_Set(FindFormUI(frmLogin, "btnExit"), PROP_UI_SHOW, r1_23)
end
function SD_LoginProc()
  -- line: [2494, 2564] id: 24
  local r0_24 = FindFormUI(frmLogin, "saveAccount")
  local r1_24 = 0
  local r2_24 = 0
  local r3_24 = SKY_Get(r0_24, PROP_UI_CHECK_CHECK)
  local r4_24 = -1
  local r5_24 = -1
  if SKY_Get(item_select, PROP_ITEM_LIST_SELECT) ~= -1 then
    local r7_24 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r4_24 = r7_24 / 10 - 1
    r1_24 = server_table[r7_24].ip_table[math.random(1, server_table[r7_24].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1043))
    return 
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1044))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r6_24 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_24, 3800)
  if r6_24 ~= 1 then
    if r6_24 == 2 then
      local r7_24 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_24, 3800)
      if r7_24 ~= 1 and r7_24 == 2 then
        local r8_24 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r1_24, 3800)
        if r8_24 ~= 1 and r8_24 == 2 then
          SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
          SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1045))
          SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
          return 
        end
      end
    elseif r6_24 == 3 then
      SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
      SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1046))
      SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
      return 
    end
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1047))
  local r7_24 = SKY_Set(VAR_APP, PROP_APP_LOGIN, 3)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  if r7_24 == 1 then
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    SKY_Set(frmLogin, PROP_FORM_REMOVE, g_igw)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SD_login_acc = 1
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r7_24 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  end
end
function CreateLoginForm()
  -- line: [2567, 2579] id: 25
  g_GAME_VISION = SKY_Get(VAR_APP, PROP_APP_GAME_VERSION)
  if g_GAME_VISION == VAR_APP_GAME_SHUN then
    CreateSunLoginForm()
  elseif g_GAME_VISION == VAR_APP_GAME_17173 then
    Create17173LoginForm()
  elseif g_GAME_VISION == VAR_APP_GAME_MOQI then
    createMoQiLoginForm()
  else
    CreateDefaultLoginForm()
  end
end
function createMoQiLoginForm()
  -- line: [2582, 2970] id: 26
  local r0_26 = SKY_Create(TYPE_UI_FORM, "frmLogin")
  SKY_Set(r0_26, PROP_PROC, "login_form_proc")
  frmLogin = r0_26
  SKY_Set(r0_26, PROP_FORM_DESKTOP, dskLogin)
  SKY_Set(r0_26, PROP_UI_POS, 0, 0, 102, 16)
  SKY_Set(r0_26, PROP_UI_SIZE, 1024, 1024)
  local r1_26 = SKY_Create(TYPE_UI_IMAGE, "imgaccountblack")
  SKY_Set(r0_26, PROP_FORM_ADD, r1_26)
  SKY_Set(r1_26, PROP_UI_SIZE, 265, 362)
  SKY_Set(r1_26, PROP_UI_POS, 381, 122)
  SKYImageLoad(SKY_Get(r1_26, PROP_UI_IMAGE), "login/back_yuanshu.dds", 265, 362, 645, 0)
  local r2_26 = SKY_Create(TYPE_UI_IMAGE, "imgxuanqu")
  SKY_Set(r0_26, PROP_FORM_ADD, r2_26)
  SKY_Set(r2_26, PROP_UI_SIZE, 323, 485)
  SKY_Set(r2_26, PROP_UI_POS, 677, 112)
  SKYImageLoad(SKY_Get(r2_26, PROP_UI_IMAGE), "login/back_yuanshumoqi.dds", 323, 490, 325, 0)
  local r3_26 = SKY_Create(TYPE_UI_IMAGE, "imgBoard")
  SKY_Set(r0_26, PROP_FORM_ADD, r3_26)
  SKY_Set(r3_26, PROP_UI_SIZE, 320, 490)
  SKY_Set(r3_26, PROP_UI_POS, 30, 113)
  SKYImageLoad(SKY_Get(r3_26, PROP_UI_IMAGE), "login/back_yuanshu.dds", 320, 490, 0, 0)
  local r4_26 = SKY_Create(TYPE_UI_LABEL, "versionlabel")
  SKY_Set(r0_26, PROP_FORM_ADD, r4_26)
  SKY_Set(r4_26, PROP_UI_POS, 517, 276)
  SKY_Set(r4_26, PROP_UI_SIZE, 115, 20)
  SKY_Set(r4_26, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r4_26, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r4_26, PROP_UI_TEXT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1048) .. VAR_VERSION)
  local r5_26 = SKY_Create(TYPE_UI_LIST, "serverchang")
  SKY_Set(r0_26, PROP_FORM_ADD, r5_26)
  SKY_Set(r5_26, PROP_UI_POS, 710, 177)
  SKY_Set(r5_26, PROP_UI_SIZE, 279, 254)
  SKY_Set(r5_26, PROP_UI_COLOR, 255, 217, 214, 199)
  SKY_Set(SKY_Get(r5_26, PROP_UI_LIST_ITEM), PROP_ITEM_LIST_FONT, DEFALUT_FONT3)
  SKY_Set(r5_26, PROP_UI_MARGIN, 0, 35, 25, 0)
  local r6_26 = SKY_Create(TYPE_UI_SCROLL, "questtalkscroll")
  SKY_Set(r5_26, PROP_UI_LIST_SCROLL, r6_26)
  SKY_Set(r6_26, PROP_UI_POS, 203, 2)
  SKY_Set(r6_26, PROP_UI_SIZE, 22, 254)
  SKY_Set(r6_26, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r6_26, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r6_26, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r6_26, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r6_26, PROP_UI_SCROLL_VALUE, 1)
  local r7_26 = SKY_Create(TYPE_UI_IMAGE, "imgblack")
  SKY_Set(r6_26, PROP_UI_INSERT_CHILD, r7_26)
  SKY_Set(r7_26, PROP_UI_SIZE, 28, 106)
  SKY_Set(r7_26, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r7_26 = SKY_Get(r7_26, PROP_UI_IMAGE)
  local r8_26 = SKY_Get(r6_26, PROP_UI_SCROLL_UP)
  SKY_Set(r8_26, PROP_UI_SIZE, 22, 19)
  SKY_Set(r8_26, PROP_UI_POS, 0, 0)
  local r9_26 = SKY_Get(r6_26, PROP_UI_SCROLL_DOWN)
  SKY_Set(r9_26, PROP_UI_SIZE, 22, 19)
  SKY_Set(r9_26, PROP_UI_POS, 0, 235)
  local r10_26 = SKY_Get(r6_26, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r10_26, PROP_UI_SIZE, 17, 41)
  SKYImageLoad(SKY_Get(r10_26, PROP_UI_IMAGE), "login/back_yuanshu.dds", 17, 41, 0, 652)
  local r12_26 = SKY_Get(r5_26, PROP_UI_LIST_ITEM)
  SKY_Set(r12_26, PROP_UI_SIZE, 0, 1)
  SKY_Set(r12_26, PROP_ITEM_LIST_SELECT_OFFSET, 0, -5)
  SKY_Set(r12_26, PROP_ITEM_LIST_FOLLOW, VAR_FALSE)
  SKY_Set(r12_26, PROP_ITEM_LIST_UNIT_SPACE, -4)
  item_select = r12_26
  local r13_26 = SKY_Get(r12_26, PROP_ITEM_LIST_SELECT_PIC)
  SKY_Set(r13_26, PROP_UI_SIZE, 0, 1)
  SKY_Set(r13_26, PROP_UI_COLOR, 100, 255, 191, 0)
  SKY_Set(r13_26, PROP_IMAGE_LOAD, "system\\white.tga", 0, 1)
  local r14_26 = SKY_Create(TYPE_UI_MEMO, "systeminfo")
  SKY_Set(r0_26, PROP_FORM_ADD, r14_26)
  SKY_Set(r14_26, PROP_UI_POS, 45, 126)
  SKY_Set(r14_26, PROP_UI_SIZE, 275, 373)
  SKY_Set(r14_26, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_26, PROP_UI_MEMO_TEXT_COLOR, 255, 0, 0, 0)
  SKY_Set(r14_26, PROP_UI_MEMO_TEXT_FONT, DEFALUT_FONT3)
  SKY_Set(r14_26, PROP_UI_MARGIN, 24, 25, 40, 25)
  SKY_Set(r14_26, PROP_UI_MEMO_LINE_SPACE, 1)
  SKY_Set(r14_26, PROP_UI_MEMO_TEXT_SHADE, VAR_FALSE)
  systeminfo = r14_26
  local r15_26 = SKY_Create(TYPE_UI_SCROLL, "systeminfoscroll")
  SKY_Set(r14_26, PROP_UI_MEMO_SCROLL, r15_26)
  SKY_Set(r15_26, PROP_UI_POS, 203, 2)
  SKY_Set(r15_26, PROP_UI_SIZE, 22, 373)
  SKY_Set(r15_26, PROP_UI_ALIGN, VAR_UI_ALIGN_RIGHT)
  SKY_Set(r15_26, PROP_UI_SCROLL_STYLE, VAR_UI_SCROLL_VERTICAL)
  SKY_Set(r15_26, PROP_UI_SCROLL_AUTO_SIZE, VAR_FALSE)
  SKY_Set(r15_26, PROP_UI_SCROLL_AUTO_HIDE, VAR_FALSE)
  SKY_Set(r15_26, PROP_UI_SCROLL_VALUE, 1)
  local r16_26 = SKY_Create(TYPE_UI_IMAGE, "systeminfo_imgblack")
  SKY_Set(r15_26, PROP_UI_INSERT_CHILD, r16_26)
  SKY_Set(r16_26, PROP_UI_SIZE, 28, 106)
  SKY_Set(r16_26, PROP_UI_ALIGN, VAR_UI_ALIGN_CLIENT)
  r16_26 = SKY_Get(r16_26, PROP_UI_IMAGE)
  local r17_26 = SKY_Get(r15_26, PROP_UI_SCROLL_UP)
  SKY_Set(r17_26, PROP_UI_SIZE, 22, 15)
  SKY_Set(r17_26, PROP_UI_POS, 0, 0)
  local r18_26 = SKY_Get(r15_26, PROP_UI_SCROLL_DOWN)
  SKY_Set(r18_26, PROP_UI_SIZE, 22, 19)
  SKY_Set(r18_26, PROP_UI_POS, 0, 355)
  local r19_26 = SKY_Get(r15_26, PROP_UI_SCROLL_SCOLL)
  SKY_Set(r19_26, PROP_UI_SIZE, 17, 40)
  SKYImageLoad(SKY_Get(r19_26, PROP_UI_IMAGE), "login/back_yuanshu.dds", 17, 40, 0, 652)
  edtAccount = CreateEdit(r0_26, "edtAccount", 177, 27, 456, 301)
  edtPassWord = CreateEdit(r0_26, "edtPassWord", 177, 27, 456, 332)
  SKY_Set(edtPassWord, PROP_UI_EDIT_PASSWORD, "*")
  local r21_26 = CreateButton(r0_26, "btnLogin", 74, 23, 436, 360)
  LoadButton(r21_26, "login/back_yuanshu.dds", 74, 23, 693, 418, 0)
  LoadButton(r21_26, "login/back_yuanshu.dds", 74, 23, 693, 455, 1)
  LoadButton(r21_26, "login/back_yuanshu.dds", 74, 23, 693, 455, 2)
  LoadButton(r21_26, "login/back_yuanshu.dds", 74, 23, 693, 455, 3)
  local r22_26 = CreateButton(r0_26, "btnExit", 74, 23, 519, 360)
  LoadButton(r22_26, "login/back_yuanshu.dds", 74, 23, 785, 418, 0)
  LoadButton(r22_26, "login/back_yuanshu.dds", 74, 23, 785, 455, 1)
  LoadButton(r22_26, "login/back_yuanshu.dds", 74, 23, 785, 455, 2)
  LoadButton(r22_26, "login/back_yuanshu.dds", 74, 23, 785, 455, 3)
  SKY_Set(r0_26, PROP_FORM_ESC_BUTTON, r22_26)
  SKY_Set(CreateButton(r0_26, "btnDragon", 98, 18, 743, 172), PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(CreateButton(r0_26, "btnlegend", 98, 18, 855, 172), PROP_UI_SHOW, VAR_FALSE)
  g_igw = SKY_Create(TYPE_UI_IGW, "igw")
  SKY_Set(g_igw, PROP_UI_POS, 0, 0)
  SKY_Set(g_igw, PROP_UI_SIZE, 10, 10)
  local r25_26 = SKY_Create(TYPE_UI_CHECK_BOX, "saveAccount")
  SKY_Set(r0_26, PROP_FORM_ADD, r25_26)
  SKY_Set(r25_26, PROP_UI_POS, 456, 420)
  SKY_Set(r25_26, PROP_UI_SIZE, 16, 17)
  SKY_Set(r25_26, PROP_UI_CHECK_ICON_SIZE, 16, 17)
  SKYImageLoad(SKY_Get(r25_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r25_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_UNCHECK_HOVER), "flydrelive\\check.tga", 16, 17, 785, 395)
  SKYImageLoad(SKY_Get(r25_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login/back_yuanshu.dds", 16, 17, 718, 395)
  SKYImageLoad(SKY_Get(r25_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login/back_yuanshu.dds", 16, 17, 718, 395)
  local r27_26 = SKY_Create(TYPE_UI_COMBO_BOX, "servercombox")
  SKY_Set(r0_26, PROP_FORM_ADD, r27_26)
  SKY_Set(r27_26, PROP_UI_POS, 834, 135)
  SKY_Set(r27_26, PROP_UI_SIZE, 111, 22)
  SKY_Set(r27_26, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r27_26, PROP_UI_COMBO_BOX_TEXT_POS, 24, 2)
  SKY_Set(r27_26, PROP_UI_COMBO_BOX_TEXT_COLOR, COLOR_GOLDEN)
  SKY_Set(r27_26, PROP_UI_FONT, DEFALUT_FONT3)
  login_combo_server_name = r27_26
  local r28_26 = SKY_Get(r27_26, PROP_UI_COMBO_BOX_MENU)
  login_combo_server_name = r28_26
  local r29_26 = SKY_Get(r28_26, PROP_UI_IMAGE)
  SKY_Set(r29_26, PROP_ITEM_RECT_STYLE, VAR_ITEM_RECT_FILL)
  SKY_Set(r29_26, PROP_UI_COLOR, 255, 0, 0, 0)
  local r30_26 = SKY_Get(r27_26, PROP_UI_COMBO_BOX_MENU)
  local r31_26 = SKY_Get(SKY_Get(r30_26, PROP_UI_MENU_ITEM), PROP_MENU_GROUP_SELECT_PIC)
  SKY_Set(r31_26, PROP_UI_COLOR, 255, 61, 61, 61)
  SKY_Set(r31_26, PROP_ITEM_RECT_COLOR, 255, 61, 61, 61)
  local r32_26 = SKY_Get(r27_26, PROP_UI_COMBO_BOX_BUTTON)
  SKY_Set(r32_26, PROP_UI_SIZE, 18, 19)
  SKY_Set(r32_26, PROP_UI_ALIGN, VAR_UI_ALIGN_LEFT_UP)
  local r33_26 = SKY_Get(r30_26, PROP_UI_MENU_ITEM)
  local r34_26 = SKY_Create(TYPE_UI_IMAGE, "imgFormImage")
  SKY_Set(r0_26, PROP_FORM_ADD, r34_26)
  SKY_Set(r34_26, PROP_UI_SIZE, 324, 290)
  SKY_Set(r34_26, PROP_UI_POS, 350, 325)
  SKY_Set(r34_26, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r34_26, PROP_UI_IMAGE), "login\\keyboard.dds", 324, 290, 0, 0)
  local r35_26 = SKY_Create(TYPE_UI_KEYBOARD, "keyboard")
  SKY_Set(r0_26, PROP_FORM_ADD, r35_26)
  SKY_Set(r35_26, PROP_UI_POS, 363, 408)
  SKY_Set(r35_26, PROP_UI_SIZE, 324, 260)
  SKY_Set(r35_26, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r35_26, PROP_UI_KBD_MAX_CHA, 20)
  SKY_Set(r35_26, PROP_UI_COLOR, 255, 0, 0, 0)
  SKY_Set(r35_26, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r35_26, PROP_UI_KBD_LOAD, "login\\keyboard.dds", 5, 10, 30, 27, 3, 3)
  keyboard = r35_26
  local r36_26 = SKY_Create(TYPE_UI_LABEL, "pasdlabel")
  SKY_Set(r0_26, PROP_FORM_ADD, r36_26)
  SKY_Set(r36_26, PROP_UI_POS, 403, 370)
  SKY_Set(r36_26, PROP_UI_SIZE, 200, 20)
  SKY_Set(r36_26, PROP_UI_FONT, DEFALUT_FONT)
  SKY_Set(r36_26, PROP_UI_COLOR, 255, 255, 244, 186)
  SKY_Set(r36_26, PROP_UI_LABEL_LINE, VAR_UI_LABEL_LINE_CENTER)
  SKY_Set(r36_26, PROP_UI_SHOW, VAR_FALSE)
  local r37_26 = CreateButton(r0_26, "btncapitalization", 20, 20, 455, 461)
  SKY_Set(r37_26, PROP_UI_SHOW, VAR_TRUE)
  SKY_Set(r37_26, PROP_UI_HINT, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1049))
  local r38_26 = CreateButton(r0_26, "btnaccount", 88, 28, 375, 579)
  SKY_Set(r38_26, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r38_26, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r38_26, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 25, 297)
  SKYImageLoad(SKY_Get(r38_26, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 25, 297)
  local r40_26 = CreateButton(r0_26, "btndelet", 60, 30, 545, 536)
  SKYImageLoad(SKY_Get(r40_26, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 60, 30, 241, 296)
  SKYImageLoad(SKY_Get(r40_26, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 60, 30, 241, 296)
  SKY_Set(r40_26, PROP_UI_SHOW, VAR_FALSE)
  local r41_26 = CreateButton(r0_26, "btnclose", 88, 28, 468, 579)
  SKY_Set(r41_26, PROP_UI_SHOW, VAR_FALSE)
  SKYImageLoad(SKY_Get(r41_26, PROP_UI_IMAGE, 1), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r41_26, PROP_UI_IMAGE, 2), "login\\keyboard.dds", 88, 28, 118, 297)
  SKYImageLoad(SKY_Get(r41_26, PROP_UI_IMAGE, 3), "login\\keyboard.dds", 88, 28, 118, 297)
  local r43_26 = SKY_Create(TYPE_UI_CHECK_BOX, "btnkeyboard")
  SKY_Set(r0_26, PROP_FORM_ADD, r43_26)
  SKY_Set(r43_26, PROP_UI_POS, 569, 585)
  SKY_Set(r43_26, PROP_UI_SHOW, VAR_FALSE)
  SKY_Set(r43_26, PROP_UI_SIZE, 17, 17)
  SKY_Set(r43_26, PROP_UI_CHECK_ICON_SIZE, 17, 17)
  SKYImageLoad(SKY_Get(r43_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK), "login\\keyboard.dds", 17, 17, 219, 302)
  SKYImageLoad(SKY_Get(r43_26, PROP_UI_IMAGE, VAR_MENU_CHECK_IMAGE_CHECK_HOVER), "login\\keyboard.dds", 17, 17, 219, 302)
end
function LoginMoQiProc(r0_27)
  -- line: [2972, 3075] id: 27
  disconnet_tag = 1
  local r1_27 = SKY_Get(SKY_Get(r0_27, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  if string.len(r1_27) == 0 then
    SKY_Set(r0_27, PROP_FORM_ACTIVE, SKY_Get(r0_27, PROP_FORM_FIND, "edtAccount"))
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1050))
    return 
  end
  local r2_27 = FindFormUI(frmLogin, "saveAccount")
  local r3_27 = 0
  local r4_27 = 0
  local r5_27 = SKY_Get(r2_27, PROP_UI_CHECK_CHECK)
  local r6_27 = -1
  local r7_27 = -1
  r7_27 = SKY_Get(item_select, PROP_ITEM_LIST_SELECT)
  if r7_27 ~= -1 then
    local r9_27 = SKY_Get(SKY_Get(item_select, PROP_ITEM_LIST_SELECT_ITEM), PROP_UI_TAG)
    r6_27 = r9_27 / 10 - 1
    r3_27 = server_table[r9_27].ip_table[math.random(1, server_table[r9_27].ip_num)]
  else
    SKY_Set(VAR_APP, PROP_APP_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1051))
    return 
  end
  local r8_27 = SKY_Get(SKY_Get(r0_27, PROP_FORM_FIND, "edtAccount"), PROP_UI_TEXT)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1052))
  SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_FALSE)
  local r9_27 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_27, 3800)
  if r9_27 == 1 then
    SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_27, r8_27, r6_27, r7_27)
  elseif r9_27 == 2 then
    local r10_27 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_27, 3800)
    if r10_27 == 1 then
      SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_27, r8_27, r6_27, r7_27)
    elseif r10_27 == 2 then
      local r11_27 = SKY_Set(VAR_NET, PROP_NET_CONNECT, r3_27, 3800)
      if r11_27 == 1 then
        SKY_Set(VAR_APP, PROP_APP_ACCOUNT_SAVE, r5_27, r8_27, r6_27, r7_27)
      elseif r11_27 == 2 then
        SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
        SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1053))
        SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
        return 
      end
    end
  elseif r9_27 == 3 then
    SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
    SKY_Set(VAR_UI_DIALOG, PROP_UI_DIALOG_MSGBOX, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1054))
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
    return 
  end
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_TRUE, SKY_Get(VAR_APP, PROP_APP_SETGET_STRMAG, 1055))
  local r10_27 = SKY_Get(SKY_Get(r0_27, PROP_FORM_FIND, "edtPassWord"), PROP_UI_TEXT)
  local r11_27 = SKY_Set(VAR_APP, PROP_APP_MOQI_LOGIN, r1_27, r10_27)
  SKY_Set(VAR_APP, PROP_APP_WAITING, VAR_FALSE)
  if r11_27 == 1 then
    SKY_Set(VAR_APP, PROP_APP_CLEAR_GAME_DATA)
    SKY_Set(g_igw, PROP_UI_IGW_VISIBLE, 0)
    SKY_Set(frmLogin, PROP_FORM_REMOVE, g_igw)
    SKY_Set(VAR_SCREEN, PROP_SCREEN_DESKTOP, dskSelect)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
  elseif r11_27 == 2 then
    SKY_Set(VAR_NET, PROP_NET_DISCONNECT)
    SKY_Set(dskLogin, PROP_DESKTOP_ALLOW_KEY, VAR_TRUE)
    SKY_Set(edtPassWord, PROP_UI_TEXT, r10_27)
    SKY_Set(frmLogin, PROP_FORM_ACTIVE, edtAccount)
  end
end