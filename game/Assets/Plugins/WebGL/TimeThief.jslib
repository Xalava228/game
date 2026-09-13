mergeInto(LibraryManager.library, {
 TT_Ready: function(){window.TimeThiefSDK.ready();},
 TT_Gameplay: function(active){window.TimeThiefSDK.gameplay(!!active);},
 TT_Ad: function(rewarded){window.TimeThiefSDK.ad(!!rewarded);},
 TT_Leaderboard: function(){window.TimeThiefSDK.leaderboard();},
 TT_Save: function(json){window.TimeThiefSDK.save(UTF8ToString(json));},
 TT_Load: function(){return stringToNewUTF8(window.TimeThiefSDK.saved||'');},
 TT_Language: function(){return stringToNewUTF8(window.TimeThiefSDK.lang||'ru');},
 TT_CanAd: function(){return window.TimeThiefSDK.sdk&&!window.TimeThiefSDK.adBusy?1:0;}
});
