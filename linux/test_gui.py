from app import *
import tempfile
from PyQt5.QtDBus import QDBusPendingCall
app=QApplication([])
QSettings.setDefaultFormat(QSettings.IniFormat)
QSettings.setPath(QSettings.IniFormat,QSettings.UserScope,tempfile.mkdtemp(prefix='uzeit-qt-check-'))
w=ClockWindow();w.show()
def check():
    try:
        for value in (1,2):
            msg=QDBusMessage.createMethodCall('org.example.Test','/test','org.example.Test','Read').createReply([QDBusVariant(value)])
            w.portal_finished(QDBusPendingCallWatcher(QDBusPendingCall.fromCompletedCall(msg)))
            assert w.portal_dark == (value==1)
        msg=QDBusMessage.createError('org.example.Unavailable','No portal')
        w.portal_finished(QDBusPendingCallWatcher(QDBusPendingCall.fromCompletedCall(msg)))
        assert w.portal_dark is False
        for full in [False,True]:
            if full != w.full:w.toggle_view()
            for theme in [1,2]:
                w.theme.setCurrentIndex(theme);app.processEvents()
                w.grab().save(f'/tmp/uzeit-linux-{full}-{theme}.png')
                assert w.theme.currentText() in ['Hell','Dunkel']
        w.set_portal(2);w.theme.setCurrentIndex(2);assert w.dark
        w.theme.setCurrentIndex(0);assert not w.dark
        w.set_portal(1);assert w.dark
        w.set_portal(2);assert not w.dark
        assert w.time.fresh,'PTB live failed: '+w.time.source
        anchor=w.time.anchor;w.time.busy=True;w.time.finish(False)
        assert w.time.source=='ZEIT FORTGEFÜHRT' and w.time.anchor==anchor
        print('PASS: vier Darstellungen, Dunkel→Auto, Systemwechsel, PTB live und Fallback')
    except Exception:
        import traceback;traceback.print_exc();app.exit(1);return
    w.close();app.quit()
QTimer.singleShot(5000,check)
sys.exit(app.exec_())
