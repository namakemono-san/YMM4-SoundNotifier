using System.ComponentModel.DataAnnotations;

namespace YMM4SoundNotifier.Settings;

public enum IdleDetection
{
    [Display(Name = "キーボードとマウスの操作が無い")]
    NoInput,

    [Display(Name = "YMM4のウィンドウが非アクティブ")]
    WindowInactive
}
