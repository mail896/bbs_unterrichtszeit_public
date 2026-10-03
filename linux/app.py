#!/usr/bin/python3
"""Unterrichtszeit: native Qt desktop application, no web engine."""
import json
import math
from pathlib import Path
import sys
import time
from datetime import datetime, timezone
from PyQt5.QtCore import Qt, QTimer, QUrl, QSettings, QRectF, QObject, pyqtSignal, pyqtSlot
from PyQt5.QtGui import QColor, QPainter, QFont, QIcon, QPalette, QPixmap, QDesktopServices
from PyQt5.QtWidgets import QApplication, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QComboBox, QFrame, QScrollArea, QSizePolicy
from PyQt5.QtNetwork import QNetworkRequest
from PyQt5.QtWebSockets import QWebSocket
from PyQt5.QtDBus import QDBusConnection, QDBusMessage, QDBusPendingCallWatcher, QDBusPendingReply, QDBusVariant
from school import SchoolClock, SLOTS

ROOT=Path(__file__).resolve().parent
ASSETS=ROOT/'assets'
YELLOW='#f5d600'; INK='#202322'

def application_icon():
    icon=QIcon()
    for size in (16,24,32,48,64,128,256,512):
        icon.addFile(str(ROOT/'icons'/f'{size}.png'))
    icon.addFile(str(ASSETS/'icon.png'))
    return icon

class WebClockCard(QFrame):
    def __init__(self):
        super().__init__()
        self.setToolTip('Web-Version / Downloads öffnen')
        self.setCursor(Qt.PointingHandCursor)
        self.setFocusPolicy(Qt.StrongFocus)
        self.setAccessibleName('Web-Version / Downloads öffnen')
    def mouseReleaseEvent(self,event):
        if event.button()==Qt.LeftButton and self.rect().contains(event.pos()):
            QDesktopServices.openUrl(QUrl('https://apps.bbs-einbeck.de/uzeit/'))
            event.accept()
        else:super().mouseReleaseEvent(event)
    def keyPressEvent(self,event):
        if event.key() in (Qt.Key_Return,Qt.Key_Enter,Qt.Key_Space):
            QDesktopServices.openUrl(QUrl('https://apps.bbs-einbeck.de/uzeit/'))
            event.accept()
        else:super().keyPressEvent(event)

class TimeSource(QObject):
    changed=pyqtSignal()
    def __init__(self,parent=None):
        super().__init__(parent); self.anchor=None; self.stamp=0; self.fresh=False; self.busy=False
        self.socket=QWebSocket(parent=self)
        self.socket.connected.connect(self.send)
        self.socket.textMessageReceived.connect(self.receive)
        self.socket.error.connect(lambda _:self.finish(False))
        self.socket.disconnected.connect(lambda:self.finish(False))
        self.timeout=QTimer(self);self.timeout.setSingleShot(True);self.timeout.timeout.connect(lambda:self.finish(False))
        self.last_wall=time.time();self.last_mono=time.monotonic()
    def now(self):
        wall,mono=time.time(),time.monotonic()
        # Reset after suspend or a significant device-time adjustment.
        if abs((wall-self.last_wall)-(mono-self.last_mono))>3:
            self.anchor=None;self.fresh=False;QTimer.singleShot(0,self.sync)
        self.last_wall,self.last_mono=wall,mono
        return datetime.fromtimestamp(self.anchor+mono-self.stamp if self.anchor is not None else wall,timezone.utc)
    @property
    def source(self):return 'PTB-ZEIT' if self.fresh and time.monotonic()-self.stamp<360 else 'ZEIT FORTGEFÜHRT' if self.anchor is not None else 'GERÄTEZEIT'
    def sync(self):
        if self.busy:return
        self.busy=True;self.timeout.start(8000)
        request=QNetworkRequest(QUrl('wss://uhr.ptb.de/time'));request.setRawHeader(b'Sec-WebSocket-Protocol',b'time')
        self.socket.open(request)
    def send(self):
        self.sent=time.monotonic();self.socket.sendTextMessage('{"c":1}')
    def receive(self,message):
        if not self.busy:return
        try:
            data=json.loads(message) if len(message)<4096 else {}
            ms=data['s'];elapsed=time.monotonic()-self.sent
            if data.get('c')!=1 or not isinstance(ms,(int,float)) or not math.isfinite(ms) or not 1577836800000<ms<4102444800000 or elapsed>3:raise ValueError()
            self.anchor=ms/1000+elapsed/2;self.stamp=time.monotonic();self.finish(True)
        except (KeyError,TypeError,ValueError):self.finish(False)
    def finish(self,success):
        if not self.busy:return
        self.busy=False;self.fresh=success;self.timeout.stop();self.socket.abort();self.changed.emit()

class Digits(QWidget):
    def __init__(self):
        super().__init__();self.value='0000';self.blink=True;self.setMinimumSize(245,125);self.setSizePolicy(QSizePolicy.Expanding,QSizePolicy.Expanding)
    def paintEvent(self,event):
        p=QPainter(self);p.setRenderHint(QPainter.Antialiasing);p.setRenderHint(QPainter.TextAntialiasing)
        scale=min(self.width()/270,self.height()/125);p.translate((self.width()-270*scale)/2,(self.height()-125*scale)/2);p.scale(scale,scale)
        for i,char in enumerate(self.value):
            x=4+i*60+(16 if i>1 else 0);rect=QRectF(x,8,54,108)
            p.setPen(Qt.NoPen);p.setBrush(QColor('#30332f'));p.drawRoundedRect(rect,6,6)
            p.setPen(QColor('#fff9e6'));font=QFont('Sans Serif');font.setPixelSize(72);font.setBold(True);p.setFont(font);p.drawText(rect,Qt.AlignCenter,char)
            p.setPen(QColor('#111511'));p.drawLine(int(x),62,int(x+54),62)
        if self.blink:
            p.setPen(QColor(YELLOW));f=QFont('Sans Serif');f.setPixelSize(45);f.setBold(True);p.setFont(f);p.drawText(QRectF(120,8,18,108),Qt.AlignCenter,':')
        p.end()

class Segments(QWidget):
    def __init__(self):super().__init__();self.progress=0;self.setFixedHeight(10)
    def paintEvent(self,event):
        p=QPainter(self);p.setPen(Qt.NoPen)
        for i in range(20):
            p.setBrush(QColor(INK if (i+1)/20<=self.progress else '#cdb600'));p.drawRect(QRectF(i*self.width()/20,0,max(1,self.width()/20-3),9))
        p.end()

def label(text='',size=13,bold=False):
    node=QLabel(text);font=node.font();font.setPointSize(size);font.setBold(bold);node.setFont(font);return node

class ClockWindow(QWidget):
    def __init__(self):
        super().__init__();self.setWindowTitle('Unterrichtszeit · BBS Einbeck');self.setWindowIcon(application_icon())
        self.settings=QSettings('BBS Einbeck','Unterrichtszeit');self.full=False;self.portal_dark=None;self.dark=False
        self.school=SchoolClock(ASSETS/'calendar.json');self.time=TimeSource(self)
        self.root=QVBoxLayout(self);self.root.setContentsMargins(16,12,16,16);self.root.setSpacing(12)
        toolbar=QHBoxLayout();self.root.addLayout(toolbar)
        self.pin=QPushButton('Anheften');self.pin.setCheckable(True);self.pin.setToolTip('Immer im Vordergrund (abhängig vom Fenstermanager)');toolbar.addWidget(self.pin)
        self.view=QPushButton('Schultag');toolbar.addWidget(self.view)
        toolbar.addWidget(QLabel('Ansicht:'));self.theme=QComboBox();self.theme.addItems(['Automatisch','Hell','Dunkel']);self.theme.setMinimumWidth(155);toolbar.addWidget(self.theme);toolbar.addStretch()
        self.header=QWidget();header=QHBoxLayout(self.header);header.setContentsMargins(0,0,0,0);self.logo=QLabel();header.addWidget(self.logo);header.addStretch();self.date=label('',11);header.addWidget(self.date);self.root.addWidget(self.header)
        columns=QHBoxLayout();columns.setSpacing(16);self.root.addLayout(columns,1)
        left=QVBoxLayout();left.setSpacing(12);columns.addLayout(left,3)
        self.clock_card=WebClockCard();self.clock_card.setObjectName('clockCard');self.clock_card.setMinimumHeight(190);card=QHBoxLayout(self.clock_card);card.setContentsMargins(18,16,18,16);card.setSpacing(22)
        self.digits=Digits();card.addWidget(self.digits,1)
        right=QVBoxLayout();card.addLayout(right,1);self.source=label('',8);self.source.setAlignment(Qt.AlignRight);right.addWidget(self.source)
        current=label('aktuell:',10);right.addWidget(current);self.status=label('',21,True);self.status.setWordWrap(True);self.status.setMinimumHeight(44);right.addWidget(self.status);self.span=label('',10);right.addWidget(self.span);self.meal=label('Mensazeit',10);right.addWidget(self.meal);right.addStretch();left.addWidget(self.clock_card)
        self.count_card=QFrame();self.count_card.setObjectName('countCard');count=QVBoxLayout(self.count_card);count.setContentsMargins(18,16,18,16);count.setSpacing(10)
        self.count_label=label('',10,True);count.addWidget(self.count_label);self.remaining=label('',27,True);count.addWidget(self.remaining);self.progress=Segments();count.addWidget(self.progress);self.note=label('',11);self.note.setWordWrap(True);count.addWidget(self.note);left.addWidget(self.count_card);left.addStretch()
        self.schedule=QScrollArea();self.schedule.setWidgetResizable(True);self.schedule.setMinimumWidth(360);self.schedule.setFrameShape(QFrame.NoFrame);body=QWidget();self.schedule.setWidget(body);sched=QVBoxLayout(body);sched.setContentsMargins(16,16,16,16);sched.addWidget(label('Dein Schultag',18,True));self.rows=[]
        for slot in SLOTS:
            row=QFrame();line=QHBoxLayout(row);line.setContentsMargins(8,7,8,7);name=label(slot.name+(' · Mensazeit' if slot.start==675 else ''),10);span=label(slot.span,10);line.addWidget(name);line.addStretch();line.addWidget(span);sched.addWidget(row);self.rows.append(row)
        self.calendar=label('',10);self.calendar.setWordWrap(True);sched.addWidget(self.calendar)
        link=QPushButton('Ferientermine Niedersachsen ↗');link.clicked.connect(lambda:QDesktopServices.openUrl(QUrl('https://www.mk.niedersachsen.de/service/ferientermine/schulferien-6491.html')));sched.addWidget(link);sched.addStretch();columns.addWidget(self.schedule,2)
        self.footer=label('© Marc Schulz · marc.schulz@bbs-einbeck.de\nLinux-Version des Visual-Basic-Unterrichtsprojektes aus 2012',9);self.root.addWidget(self.footer)
        self.theme.setCurrentIndex({'auto':0,'light':1,'dark':2}.get(str(self.settings.value('theme','auto')),0))
        self.theme.currentIndexChanged.connect(self.change_theme);self.pin.toggled.connect(self.set_pin);self.view.clicked.connect(self.toggle_view)
        self.header.hide();self.schedule.hide();self.footer.hide();self.resize(720,445);self.setMinimumSize(680,430)
        self.timer=QTimer(self);self.timer.timeout.connect(self.refresh);self.timer.start(250)
        self.sync_timer=QTimer(self);self.sync_timer.timeout.connect(self.time.sync);self.sync_timer.start(300000)
        self.apply_theme();self.refresh();QTimer.singleShot(0,self.time.sync)
        QTimer.singleShot(0,self.read_portal)
        if str(self.settings.value('pinned','false')).lower()=='true':self.pin.setChecked(True)
        QApplication.instance().paletteChanged.connect(lambda _:self.apply_theme())
    def read_portal(self):
        bus=QDBusConnection.sessionBus()
        bus.connect('org.freedesktop.portal.Desktop','/org/freedesktop/portal/desktop','org.freedesktop.portal.Settings','SettingChanged',self.portal_changed)
        msg=QDBusMessage.createMethodCall('org.freedesktop.portal.Desktop','/org/freedesktop/portal/desktop','org.freedesktop.portal.Settings','Read');msg.setArguments(['org.freedesktop.appearance','color-scheme'])
        self.portal_call=QDBusPendingCallWatcher(bus.asyncCall(msg,1500),self);self.portal_call.finished.connect(self.portal_finished)
    def portal_finished(self,watcher):
        try:
            reply=QDBusPendingReply(watcher)
            if not reply.isError() and reply.isValid():
                self.set_portal(reply.value())
        finally:
            watcher.deleteLater()
    @pyqtSlot(str,str,QDBusVariant)
    def portal_changed(self,namespace,key,value):
        if namespace=='org.freedesktop.appearance' and key=='color-scheme':self.set_portal(value)
    def set_portal(self,value):
        while isinstance(value,QDBusVariant):value=value.variant()
        self.portal_dark=True if value==1 else False if value==2 else None;self.apply_theme()
    def change_theme(self):self.settings.setValue('theme',['auto','light','dark'][self.theme.currentIndex()]);self.apply_theme()
    def apply_theme(self):
        self.dark=self.theme.currentIndex()==2 or (self.theme.currentIndex()==0 and (self.portal_dark if self.portal_dark is not None else QApplication.palette().color(QPalette.Window).lightness()<128))
        fg='#f1f2e9' if self.dark else INK;paper='#141615' if self.dark else '#f6f6f3';surface='#292e2a' if self.dark else '#ffffff'
        self.setStyleSheet(f'ClockWindow {{background:{paper};color:{fg}}} QLabel {{color:{fg};background:transparent}} QFrame#clockCard {{background:{INK};border-radius:16px}} QFrame#clockCard QLabel {{color:#bec5be}} QFrame#countCard {{background:{YELLOW};border-radius:16px}} QFrame#countCard QLabel {{color:{INK}}} QPushButton,QComboBox {{background:{surface};color:{fg};border:1px solid #777;border-radius:6px;padding:7px}} QPushButton:checked {{background:{YELLOW};color:{INK}}} QScrollArea,QScrollArea>QWidget>QWidget {{background:{surface};border-radius:12px}}')
        self.status.setStyleSheet(f'color:{YELLOW}');self.logo.setPixmap(QPixmap(str(ASSETS/('bbs-logo-dark.png' if self.dark else 'bbs-logo.png'))).scaled(240,65,Qt.KeepAspectRatio,Qt.SmoothTransformation));self.refresh()
    def set_pin(self,enabled):
        self.settings.setValue('pinned',enabled);self.pin.setText('Angeheftet' if enabled else 'Anheften');self.setWindowFlag(Qt.WindowStaysOnTopHint,enabled);self.show()
    def toggle_view(self):
        self.full=not self.full;self.view.setText('Kompakt' if self.full else 'Schultag')
        for item in [self.header,self.schedule,self.footer]:item.setVisible(self.full)
        self.setMinimumSize(980 if self.full else 680,560 if self.full else 430)
        screen=self.screen().availableGeometry();self.resize(min(1120,screen.width()) if self.full else 720,min(760,screen.height()) if self.full else 445)
    def refresh(self):
        s=self.school.evaluate(self.time.now());self.state=s
        self.digits.value=s['now'].strftime('%H%M');self.digits.blink=s['now'].microsecond<500000;self.digits.update()
        self.digits.setAccessibleName('Uhrzeit '+s['now'].strftime('%H:%M'))
        self.source.setText(self.time.source);self.status.setText(s['status']);self.span.setText(s['slot'].span+' Uhr' if s['slot'] else '')
        self.meal.setVisible(s['index']==5);self.note.setText(s['note']);active=s['remaining'] is not None
        self.count_label.setVisible(active);self.progress.setVisible(active)
        self.count_label.setText(s['label'].upper());self.remaining.setText(f"{s['remaining']//60:02} Minuten   {s['remaining']%60:02} Sekunden" if active else s['greeting'])
        self.progress.progress=s['progress'];self.progress.update();self.progress.setAccessibleName(f"{int(s['progress']*100)} Prozent vergangen")
        self.calendar.setText(s['calendar']);self.date.setText(['Montag','Dienstag','Mittwoch','Donnerstag','Freitag','Samstag','Sonntag'][s['now'].weekday()]+s['now'].strftime(', %d.%m.%Y'))
        key=(s['index'],self.dark)
        if getattr(self,'row_state',None)!=key:
            self.row_state=key
            for i,row in enumerate(self.rows):
                selected=i==s['index'];bg=INK if selected else '#303631' if self.dark and SLOTS[i].pause else '#f1f3ee' if SLOTS[i].pause else 'transparent';fg=YELLOW if selected else '#f1f2e9' if self.dark else INK
                row.setStyleSheet(f'QFrame {{background:{bg};border-radius:6px}} QLabel {{color:{fg}}}')
    def closeEvent(self,event):self.time.socket.abort();self.settings.sync();super().closeEvent(event)

def main():
    QApplication.setAttribute(Qt.AA_EnableHighDpiScaling);QApplication.setAttribute(Qt.AA_UseHighDpiPixmaps)
    app=QApplication(['unterrichtszeit', '-name', 'unterrichtszeit', *sys.argv[1:]]);app.setApplicationName('Unterrichtszeit');app.setDesktopFileName('de.bbs-einbeck.unterrichtszeit');app.setWindowIcon(application_icon())
    window=ClockWindow();window.show();sys.exit(app.exec_())
if __name__=='__main__':main()
