namespace BeanMeUp;

/// <summary>
/// Ergänzende Texte (Version 2.2: Update-Prüfung). Gleiches Format wie in Strings.cs;
/// Schlüssel, die es dort schon gibt (z. B. help.portable.b), werden hier überschrieben.
/// </summary>
static partial class L
{
    const string De2 = """
set.updates=Täglich nach Updates suchen
set.checkNow=Jetzt prüfen
set.tipUpdates=Fragt einmal täglich bei GitHub nach einer neuen Version. Es werden keine persönlichen Daten gesendet; heruntergeladen oder installiert wird nichts automatisch.
upd.balloonTitle={0} {1} ist verfügbar
upd.balloonText=Klicke hier, um die Download-Seite zu öffnen.
upd.footer=Neue Version {0} verfügbar – herunterladen
upd.current=Du verwendest bereits die neueste Version ({0}).
upd.newer=Version {0} ist verfügbar.\n\nDownload-Seite jetzt öffnen?
upd.failed=Die Update-Prüfung hat nicht geklappt:\n{0}
help.update.t=Updates
help.update.b=Bean me up fragt einmal täglich bei GitHub nach, ob es eine neue Version gibt. Dabei werden keine persönlichen Daten gesendet – GitHub sieht nur deine IP-Adresse, wie bei jedem Webseitenaufruf. Gibt es eine neue Version, erscheint ein Hinweis; ein Klick öffnet die Download-Seite. Abschaltbar unter „Weitere Einstellungen“. Sonst nutzt Bean me up keine Internetverbindung.
help.portable.b=Keine Installation, keine Adminrechte, keine Datensammlung. Die Einstellungen liegen in „BeanMeUp.ini“ neben der EXE. Zum Entfernen „Mit Windows starten“ ausschalten und die EXE löschen.
""";

    const string En2 = """
set.updates=Check for updates daily
set.checkNow=Check now
set.tipUpdates=Asks GitHub once a day whether a new version exists. No personal data is sent; nothing is downloaded or installed automatically.
upd.balloonTitle={0} {1} is available
upd.balloonText=Click here to open the download page.
upd.footer=New version {0} available – download
upd.current=You are already using the latest version ({0}).
upd.newer=Version {0} is available.\n\nOpen the download page now?
upd.failed=The update check did not work:\n{0}
help.update.t=Updates
help.update.b=Bean me up asks GitHub once a day whether there is a new version. No personal data is sent – GitHub only sees your IP address, as with any website visit. If there is a new version, a notice appears; a click opens the download page. Can be turned off under "More settings". Otherwise Bean me up does not use the internet.
help.portable.b=No installation, no admin rights, no data collection. Settings are stored in "BeanMeUp.ini" next to the EXE. To remove: turn off "Start with Windows" and delete the EXE.
""";

    const string Fr2 = """
set.updates=Rechercher les mises à jour chaque jour
set.checkNow=Vérifier maintenant
set.tipUpdates=Demande une fois par jour à GitHub s'il existe une nouvelle version. Aucune donnée personnelle n'est envoyée ; rien n'est téléchargé ni installé automatiquement.
upd.balloonTitle={0} {1} est disponible
upd.balloonText=Cliquez ici pour ouvrir la page de téléchargement.
upd.footer=Nouvelle version {0} disponible – télécharger
upd.current=Vous utilisez déjà la dernière version ({0}).
upd.newer=La version {0} est disponible.\n\nOuvrir la page de téléchargement ?
upd.failed=La recherche de mises à jour a échoué :\n{0}
help.update.t=Mises à jour
help.update.b=Bean me up demande une fois par jour à GitHub s'il existe une nouvelle version. Aucune donnée personnelle n'est envoyée – GitHub voit seulement votre adresse IP, comme lors de toute visite d'un site. Si une nouvelle version existe, un avis s'affiche ; un clic ouvre la page de téléchargement. Désactivable dans « Plus de paramètres ». Sinon, Bean me up n'utilise pas Internet.
help.portable.b=Aucune installation, aucun droit administrateur, aucune collecte de données. Les paramètres sont enregistrés dans « BeanMeUp.ini » à côté de l'EXE. Pour supprimer : désactivez « Démarrer avec Windows » et supprimez l'EXE.
""";

    const string Es2 = """
set.updates=Buscar actualizaciones a diario
set.checkNow=Comprobar ahora
set.tipUpdates=Pregunta a GitHub una vez al día si hay una versión nueva. No se envían datos personales; no se descarga ni instala nada automáticamente.
upd.balloonTitle={0} {1} está disponible
upd.balloonText=Haz clic aquí para abrir la página de descarga.
upd.footer=Nueva versión {0} disponible – descargar
upd.current=Ya usas la versión más reciente ({0}).
upd.newer=La versión {0} está disponible.\n\n¿Abrir ahora la página de descarga?
upd.failed=No se pudo comprobar si hay actualizaciones:\n{0}
help.update.t=Actualizaciones
help.update.b=Bean me up pregunta a GitHub una vez al día si hay una versión nueva. No se envían datos personales: GitHub solo ve tu dirección IP, como en cualquier visita a una web. Si hay una versión nueva aparece un aviso; un clic abre la página de descarga. Se puede desactivar en «Más ajustes». Aparte de esto, Bean me up no usa Internet.
help.portable.b=Sin instalación, sin derechos de administrador y sin recopilar datos. Los ajustes se guardan en «BeanMeUp.ini» junto al EXE. Para quitarlo: desactiva «Iniciar con Windows» y borra el EXE.
""";

    const string It2 = """
set.updates=Cerca aggiornamenti ogni giorno
set.checkNow=Controlla ora
set.tipUpdates=Chiede a GitHub una volta al giorno se esiste una nuova versione. Non vengono inviati dati personali; nulla viene scaricato o installato automaticamente.
upd.balloonTitle={0} {1} è disponibile
upd.balloonText=Fai clic qui per aprire la pagina di download.
upd.footer=Nuova versione {0} disponibile – scarica
upd.current=Stai già usando la versione più recente ({0}).
upd.newer=La versione {0} è disponibile.\n\nAprire ora la pagina di download?
upd.failed=Il controllo degli aggiornamenti non è riuscito:\n{0}
help.update.t=Aggiornamenti
help.update.b=Bean me up chiede a GitHub una volta al giorno se esiste una nuova versione. Non vengono inviati dati personali: GitHub vede solo il tuo indirizzo IP, come per qualsiasi sito web. Se c'è una nuova versione compare un avviso; un clic apre la pagina di download. Disattivabile in «Altre impostazioni». Per il resto Bean me up non usa Internet.
help.portable.b=Nessuna installazione, nessun diritto di amministratore, nessuna raccolta di dati. Le impostazioni sono salvate in «BeanMeUp.ini» accanto all'EXE. Per rimuoverlo: disattiva «Avvia con Windows» ed elimina l'EXE.
""";

    const string Pt2 = """
set.updates=Procurar atualizações diariamente
set.checkNow=Verificar agora
set.tipUpdates=Pergunta ao GitHub uma vez por dia se existe uma nova versão. Nenhum dado pessoal é enviado; nada é baixado ou instalado automaticamente.
upd.balloonTitle={0} {1} está disponível
upd.balloonText=Clique aqui para abrir a página de download.
upd.footer=Nova versão {0} disponível – baixar
upd.current=Você já está usando a versão mais recente ({0}).
upd.newer=A versão {0} está disponível.\n\nAbrir a página de download agora?
upd.failed=A verificação de atualizações falhou:\n{0}
help.update.t=Atualizações
help.update.b=O Bean me up pergunta ao GitHub uma vez por dia se existe uma nova versão. Nenhum dado pessoal é enviado – o GitHub vê apenas seu endereço IP, como em qualquer site. Se houver uma nova versão, aparece um aviso; um clique abre a página de download. Pode ser desligado em "Mais configurações". Fora isso, o Bean me up não usa a internet.
help.portable.b=Sem instalação, sem direitos de administrador, sem coleta de dados. As configurações ficam em "BeanMeUp.ini" ao lado do EXE. Para remover: desligue "Iniciar com o Windows" e apague o EXE.
""";

    const string Nl2 = """
set.updates=Dagelijks controleren op updates
set.checkNow=Nu controleren
set.tipUpdates=Vraagt eenmaal per dag aan GitHub of er een nieuwe versie is. Er worden geen persoonlijke gegevens verzonden; er wordt niets automatisch gedownload of geïnstalleerd.
upd.balloonTitle={0} {1} is beschikbaar
upd.balloonText=Klik hier om de downloadpagina te openen.
upd.footer=Nieuwe versie {0} beschikbaar – downloaden
upd.current=Je gebruikt al de nieuwste versie ({0}).
upd.newer=Versie {0} is beschikbaar.\n\nDownloadpagina nu openen?
upd.failed=Controleren op updates is mislukt:\n{0}
help.update.t=Updates
help.update.b=Bean me up vraagt eenmaal per dag aan GitHub of er een nieuwe versie is. Er worden geen persoonlijke gegevens verzonden – GitHub ziet alleen je IP-adres, zoals bij elk websitebezoek. Is er een nieuwe versie, dan verschijnt een melding; een klik opent de downloadpagina. Uit te zetten via 'Meer instellingen'. Verder gebruikt Bean me up geen internet.
help.portable.b=Geen installatie, geen beheerdersrechten, geen gegevensverzameling. Instellingen staan in 'BeanMeUp.ini' naast de EXE. Verwijderen: 'Starten met Windows' uitzetten en de EXE wissen.
""";

    const string Pl2 = """
set.updates=Codziennie sprawdzaj aktualizacje
set.checkNow=Sprawdź teraz
set.tipUpdates=Raz dziennie pyta GitHub, czy jest nowa wersja. Nie są wysyłane żadne dane osobowe; nic nie jest automatycznie pobierane ani instalowane.
upd.balloonTitle={0} {1} jest dostępny
upd.balloonText=Kliknij tutaj, aby otworzyć stronę pobierania.
upd.footer=Nowa wersja {0} dostępna – pobierz
upd.current=Używasz już najnowszej wersji ({0}).
upd.newer=Dostępna jest wersja {0}.\n\nOtworzyć teraz stronę pobierania?
upd.failed=Sprawdzanie aktualizacji nie powiodło się:\n{0}
help.update.t=Aktualizacje
help.update.b=Bean me up raz dziennie pyta GitHub, czy jest nowa wersja. Nie są wysyłane żadne dane osobowe – GitHub widzi tylko Twój adres IP, jak przy każdej wizycie na stronie. Gdy pojawi się nowa wersja, zobaczysz powiadomienie; kliknięcie otwiera stronę pobierania. Można to wyłączyć w „Więcej ustawień”. Poza tym Bean me up nie korzysta z internetu.
help.portable.b=Bez instalacji, bez uprawnień administratora, bez zbierania danych. Ustawienia są zapisywane w „BeanMeUp.ini” obok pliku EXE. Aby usunąć: wyłącz „Uruchamiaj z Windows” i usuń plik EXE.
""";

    const string Cs2 = """
set.updates=Denně hledat aktualizace
set.checkNow=Zkontrolovat
set.tipUpdates=Jednou denně se zeptá GitHubu, zda existuje nová verze. Neodesílají se žádné osobní údaje; nic se automaticky nestahuje ani neinstaluje.
upd.balloonTitle={0} {1} je k dispozici
upd.balloonText=Kliknutím otevřete stránku ke stažení.
upd.footer=Nová verze {0} k dispozici – stáhnout
upd.current=Už používáte nejnovější verzi ({0}).
upd.newer=Je k dispozici verze {0}.\n\nOtevřít stránku ke stažení?
upd.failed=Kontrola aktualizací se nezdařila:\n{0}
help.update.t=Aktualizace
help.update.b=Bean me up se jednou denně zeptá GitHubu, zda existuje nová verze. Neodesílají se žádné osobní údaje – GitHub vidí jen vaši IP adresu jako při každé návštěvě webu. Je-li k dispozici nová verze, zobrazí se upozornění; kliknutím otevřete stránku ke stažení. Lze vypnout v „Další nastavení“. Jinak Bean me up internet nepoužívá.
help.portable.b=Bez instalace, bez práv správce, bez sběru dat. Nastavení je uloženo v „BeanMeUp.ini“ vedle EXE. Odstranění: vypněte „Spouštět s Windows“ a smažte EXE.
""";

    const string Sv2 = """
set.updates=Sök efter uppdateringar dagligen
set.checkNow=Sök nu
set.tipUpdates=Frågar GitHub en gång om dagen om det finns en ny version. Inga personuppgifter skickas; inget laddas ned eller installeras automatiskt.
upd.balloonTitle={0} {1} finns tillgänglig
upd.balloonText=Klicka här för att öppna nedladdningssidan.
upd.footer=Ny version {0} finns – ladda ned
upd.current=Du använder redan den senaste versionen ({0}).
upd.newer=Version {0} finns tillgänglig.\n\nÖppna nedladdningssidan nu?
upd.failed=Sökningen efter uppdateringar misslyckades:\n{0}
help.update.t=Uppdateringar
help.update.b=Bean me up frågar GitHub en gång om dagen om det finns en ny version. Inga personuppgifter skickas – GitHub ser bara din IP-adress, som vid alla webbplatsbesök. Finns en ny version visas ett meddelande; ett klick öppnar nedladdningssidan. Kan stängas av under ”Fler inställningar”. I övrigt använder Bean me up inte internet.
help.portable.b=Ingen installation, inga administratörsrättigheter, ingen datainsamling. Inställningarna sparas i ”BeanMeUp.ini” bredvid EXE-filen. Ta bort: stäng av ”Starta med Windows” och radera EXE-filen.
""";

    const string Tr2 = """
set.updates=Güncellemeleri her gün denetle
set.checkNow=Şimdi denetle
set.tipUpdates=Günde bir kez GitHub'a yeni bir sürüm olup olmadığını sorar. Kişisel veri gönderilmez; hiçbir şey otomatik olarak indirilmez veya kurulmaz.
upd.balloonTitle={0} {1} kullanılabilir
upd.balloonText=İndirme sayfasını açmak için buraya tıklayın.
upd.footer=Yeni sürüm {0} hazır – indir
upd.current=Zaten en yeni sürümü ({0}) kullanıyorsunuz.
upd.newer={0} sürümü kullanılabilir.\n\nİndirme sayfası şimdi açılsın mı?
upd.failed=Güncelleme denetimi başarısız oldu:\n{0}
help.update.t=Güncellemeler
help.update.b=Bean me up günde bir kez GitHub'a yeni bir sürüm olup olmadığını sorar. Kişisel veri gönderilmez – GitHub, her web sitesi ziyaretinde olduğu gibi yalnızca IP adresinizi görür. Yeni bir sürüm varsa bir bildirim görünür; tıklamak indirme sayfasını açar. "Diğer ayarlar"dan kapatılabilir. Bunun dışında Bean me up internet kullanmaz.
help.portable.b=Kurulum yok, yönetici hakkı yok, veri toplama yok. Ayarlar EXE'nin yanındaki "BeanMeUp.ini" dosyasında saklanır. Kaldırmak için: "Windows ile başlat"ı kapatın ve EXE'yi silin.
""";

    const string Ru2 = """
set.updates=Ежедневно проверять обновления
set.checkNow=Проверить сейчас
set.tipUpdates=Раз в день спрашивает GitHub о новой версии. Личные данные не отправляются; ничего не скачивается и не устанавливается автоматически.
upd.balloonTitle=Доступна версия {0} {1}
upd.balloonText=Щёлкните здесь, чтобы открыть страницу загрузки.
upd.footer=Доступна версия {0} – скачать
upd.current=У вас уже последняя версия ({0}).
upd.newer=Доступна версия {0}.\n\nОткрыть страницу загрузки?
upd.failed=Не удалось проверить обновления:\n{0}
help.update.t=Обновления
help.update.b=Раз в день Bean me up спрашивает GitHub, есть ли новая версия. Личные данные не отправляются – GitHub видит только ваш IP-адрес, как при любом посещении сайта. Если вышла новая версия, появится уведомление; щелчок откроет страницу загрузки. Можно отключить в «Другие настройки». В остальном Bean me up не использует интернет.
help.portable.b=Без установки, без прав администратора, без сбора данных. Настройки хранятся в «BeanMeUp.ini» рядом с EXE. Удаление: выключите «Запуск с Windows» и удалите EXE.
""";

    const string Uk2 = """
set.updates=Щодня перевіряти оновлення
set.checkNow=Перевірити зараз
set.tipUpdates=Раз на день запитує GitHub про нову версію. Особисті дані не надсилаються; нічого не завантажується й не встановлюється автоматично.
upd.balloonTitle=Доступна версія {0} {1}
upd.balloonText=Клацніть тут, щоб відкрити сторінку завантаження.
upd.footer=Доступна версія {0} – завантажити
upd.current=У вас уже остання версія ({0}).
upd.newer=Доступна версія {0}.\n\nВідкрити сторінку завантаження?
upd.failed=Не вдалося перевірити оновлення:\n{0}
help.update.t=Оновлення
help.update.b=Раз на день Bean me up запитує GitHub, чи є нова версія. Особисті дані не надсилаються – GitHub бачить лише вашу IP-адресу, як під час будь-якого відвідування сайту. Якщо вийшла нова версія, з'явиться сповіщення; клацання відкриє сторінку завантаження. Можна вимкнути в «Інші налаштування». В іншому Bean me up не використовує інтернет.
help.portable.b=Без встановлення, без прав адміністратора, без збору даних. Налаштування зберігаються в «BeanMeUp.ini» поруч з EXE. Видалення: вимкніть «Запуск із Windows» і видаліть EXE.
""";

    const string Ja2 = """
set.updates=毎日更新を確認する
set.checkNow=今すぐ確認
set.tipUpdates=1 日 1 回 GitHub に新しいバージョンがあるか問い合わせます。個人データは送信されず、自動でダウンロードやインストールは行いません。
upd.balloonTitle={0} {1} が利用可能です
upd.balloonText=ここをクリックするとダウンロード ページが開きます。
upd.footer=新しいバージョン {0} – ダウンロード
upd.current=最新バージョン ({0}) を使用しています。
upd.newer=バージョン {0} が利用可能です。\n\nダウンロード ページを開きますか?
upd.failed=更新を確認できませんでした:\n{0}
help.update.t=更新
help.update.b=Bean me up は 1 日 1 回 GitHub に新しいバージョンがあるか問い合わせます。個人データは送信されません。GitHub に見えるのは、通常の Web サイト閲覧と同じく IP アドレスだけです。新しいバージョンがあると通知が表示され、クリックするとダウンロード ページが開きます。「その他の設定」でオフにできます。それ以外に Bean me up はインターネットを使いません。
help.portable.b=インストール不要、管理者権限不要、データ収集なし。設定は EXE と同じ場所の「BeanMeUp.ini」に保存されます。削除するには「Windows と同時に起動」をオフにして EXE を削除します。
""";

    const string Zh2 = """
set.updates=每天检查更新
set.checkNow=立即检查
set.tipUpdates=每天向 GitHub 询问一次是否有新版本。不会发送任何个人数据，也不会自动下载或安装。
upd.balloonTitle={0} {1} 已发布
upd.balloonText=单击此处打开下载页面。
upd.footer=新版本 {0} 已发布 – 下载
upd.current=你已在使用最新版本（{0}）。
upd.newer=版本 {0} 已发布。\n\n现在打开下载页面吗？
upd.failed=检查更新失败：\n{0}
help.update.t=更新
help.update.b=Bean me up 每天向 GitHub 询问一次是否有新版本。不会发送任何个人数据——与访问任何网站一样，GitHub 只能看到你的 IP 地址。如有新版本会显示提示，单击即可打开下载页面。可在“更多设置”中关闭。除此之外，Bean me up 不使用互联网。
help.portable.b=无需安装、无需管理员权限、不收集数据。设置保存在 EXE 旁边的“BeanMeUp.ini”中。删除方法：关闭“随 Windows 启动”，然后删除 EXE。
""";

    const string Ko2 = """
set.updates=매일 업데이트 확인
set.checkNow=지금 확인
set.tipUpdates=하루에 한 번 GitHub에 새 버전이 있는지 확인합니다. 개인 데이터는 전송되지 않으며 자동으로 다운로드하거나 설치하지 않습니다.
upd.balloonTitle={0} {1} 사용 가능
upd.balloonText=다운로드 페이지를 열려면 여기를 클릭하세요.
upd.footer=새 버전 {0} – 다운로드
upd.current=이미 최신 버전({0})을 사용 중입니다.
upd.newer=버전 {0}을(를) 사용할 수 있습니다.\n\n지금 다운로드 페이지를 열까요?
upd.failed=업데이트를 확인하지 못했습니다:\n{0}
help.update.t=업데이트
help.update.b=Bean me up은 하루에 한 번 GitHub에 새 버전이 있는지 확인합니다. 개인 데이터는 전송되지 않으며, 일반적인 웹사이트 방문처럼 GitHub는 IP 주소만 볼 수 있습니다. 새 버전이 있으면 알림이 표시되고, 클릭하면 다운로드 페이지가 열립니다. '추가 설정'에서 끌 수 있습니다. 그 외에는 인터넷을 사용하지 않습니다.
help.portable.b=설치, 관리자 권한이 필요 없으며 데이터를 수집하지 않습니다. 설정은 EXE 옆의 'BeanMeUp.ini'에 저장됩니다. 제거하려면 'Windows와 함께 시작'을 끄고 EXE를 삭제하세요.
""";
}
