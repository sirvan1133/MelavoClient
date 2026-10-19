// Reuse the native bridge and real data. No prototype traffic or test simulation.
const portraitMain=document.querySelector('main.main-content');
const portraitHeader=document.createElement('header');portraitHeader.className='portrait-header';
portraitHeader.innerHTML='<div class="portrait-brand"><span class="portrait-mark"><svg width="24" height="24" viewBox="0 0 32 32" fill="none"><path d="M5 25V8l11 12L27 8v17" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/></svg></span>Melavo</div><div class="portrait-tools"><button id="portraitLanguage" type="button">EN</button><button id="portraitTheme" type="button">◐</button><button id="portraitSettings" type="button">⚙</button></div>';
portraitMain.prepend(portraitHeader);
const portraitStatus=document.createElement('div');portraitStatus.className='portrait-status';portraitStatus.innerHTML='<span id="portraitStatusText"></span><button class="portrait-mode" id="portraitMode">TUN</button>';portraitHeader.after(portraitStatus);
const portraitHome=$('page-servers'),portraitSheet=document.createElement('section');portraitSheet.className='portrait-sheet';portraitSheet.id='portraitServerSheet';
portraitSheet.innerHTML='<div class="portrait-sheet-heading"><strong id="portraitSheetTitle"></strong><button type="button" class="portrait-back" id="portraitCloseServers"></button></div>';
for(const element of [...portraitHome.children])if(element.matches('.server-actions,.tabs,#hostStatus,#serverListContainer'))portraitSheet.append(element);
portraitHome.append(portraitSheet);
const portraitMetrics=document.querySelector('.connection-stats');portraitMetrics.className='portrait-metrics';$('connectionPanel').after(portraitMetrics);
const portraitSelected=document.createElement('button');portraitSelected.type='button';portraitSelected.className='portrait-server';portraitSelected.id='portraitSelectedServer';portraitSelected.innerHTML='<span class="server-emblem">◎</span><span class="selected-copy"><strong id="portraitServerName"></strong><small id="portraitServerHint"></small></span><span>⌄</span>';portraitMetrics.after(portraitSelected);
const portraitFooter=document.createElement('div');portraitFooter.className='portrait-footer';portraitFooter.innerHTML='<button id="portraitSubscriptions"></button><button id="portraitServers"></button>';portraitSelected.after(portraitFooter);
const portraitIdentity=document.createElement('div');portraitIdentity.className='portrait-identity';portraitIdentity.textContent='Melavo VPN · Xray + sing-box';portraitFooter.after(portraitIdentity);
const portraitDetails=document.createElement('div');portraitDetails.className='portrait-session-details';for(const id of ['statUptime','statData'])portraitDetails.append($(id).parentElement);$('page-singbox').prepend(portraitDetails);$('page-singbox').append($('ipPanel'));
const portraitSettingsBack=document.createElement('button');portraitSettingsBack.type='button';portraitSettingsBack.className='portrait-back';portraitSettingsBack.id='portraitSettingsBack';$('settingsHub').prepend(portraitSettingsBack);
function portraitHomeView(){document.body.dataset.portraitPanel='home';showPage('servers');}
function portraitOpenServers(view='all'){showPage('servers');document.body.dataset.portraitPanel='servers';filterView(view);requestAnimationFrame(()=>$('searchInput').focus());}
showServerList=()=>portraitOpenServers();
$('portraitSelectedServer').onclick=()=>portraitOpenServers();$('portraitServers').onclick=()=>portraitOpenServers();
$('portraitSubscriptions').onclick=()=>portraitOpenServers('subs');$('portraitCloseServers').onclick=portraitHomeView;$('portraitSettingsBack').onclick=portraitHomeView;
$('portraitSettings').onclick=()=>{document.body.dataset.portraitPanel='settings';showPage('singbox');};
$('portraitTheme').onclick=()=>{document.body.dataset.portraitPanel='settings';showPage('settings');};
$('portraitLanguage').onclick=()=>setLang(currentLang==='en'?'fa':'en');
$('portraitMode').onclick=()=>{document.body.dataset.portraitPanel='settings';showPage('singbox');};
const portraitOriginalLanguage=setLang;setLang=function(...args){portraitOriginalLanguage(...args);const fa=currentLang==='fa';setText('portraitLanguage',fa?'EN':'FA');setText('portraitSheetTitle',fa?'سرورها و اشتراک‌ها':'Servers & subscriptions');setText('portraitCloseServers',fa?'بازگشت':'Back');setText('portraitSettingsBack',fa?'بازگشت به خانه':'Back to home');setText('portraitServers',fa?'◎ انتخاب سرور':'◎ Select server');setText('portraitSubscriptions',fa?'＋ مدیریت اشتراک':'＋ Subscriptions');$('portraitSettings').setAttribute('aria-label',fa?'تنظیمات':'Settings');$('portraitTheme').setAttribute('aria-label',fa?'ظاهر و تم':'Appearance');if(hostState)portraitSync(hostState);};
function portraitSync(state){setText('portraitStatusText',t(state.connected?'connected':state.connecting?'connecting':state.disconnecting?'disconnecting':'disconnected'));setText('portraitMode',state.tun?'TUN':'SOCKS');setText('portraitServerName',state.serverName||t('select_first'));setText('portraitServerHint',state.connected?(state.ip||'—'):(currentLang==='fa'?'برای انتخاب یا افزودن سرور بزنید':'Choose or import a configuration'));$('connectionPanel').classList.toggle('connecting',!!state.connecting);portraitIdentity.textContent='Melavo VPN '+state.version+' · Xray + sing-box';}
const portraitOriginalLive=applyLiveState;applyLiveState=function(state){portraitOriginalLive(state);portraitSync(state);};
const portraitOriginalSelect=selectNode;selectNode=function(...args){const result=portraitOriginalSelect(...args);portraitHomeView();return result;};
document.addEventListener('keydown',event=>{if(event.key==='Escape'&&!document.querySelector('.modal-overlay.show'))portraitHomeView();});
document.body.dataset.portraitPanel='home';
