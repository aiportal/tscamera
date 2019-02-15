
/*-----------------------------------------------------------------------------
	Table
-----------------------------------------------------------------------------*/

CREATE TABLE [SystemConfig] (
    [ConfigID] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    [Subject] nvarchar(50) NOT NULL COLLATE NOCASE DEFAULT Global,
    [ItemName] nvarchar(50) NOT NULL,
    [ItemValue] nvarchar(255),
    [ItemDesc] nvarchar(255),
    [ValueType] nvarchar(50)
);

CREATE TABLE [ApplicationInfo] (
    [ApplicationId] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    [ProcessName] nvarchar(255) NOT NULL COLLATE NOCASE UNIQUE,
    [FileName] nvarchar(255) COLLATE NOCASE,
    [Description] nvarchar(255),
    [HasWindow] bit NOT NULL DEFAULT 1,
    [IconData] image
);

CREATE TABLE [HostInfo] (
    [HostId] integer PRIMARY KEY AUTOINCREMENT NOT NULL,
    [HostUrl] nvarchar(255) NOT NULL COLLATE NOCASE UNIQUE,
    [HostName] nvarchar(50),
    [Description] nvarchar(255)
);
CREATE INDEX [IX_HostUrl] ON [HostInfo] ([HostUrl]);

CREATE TABLE [SessionInfo] (
    [SessionId] guid PRIMARY KEY NOT NULL,
    [CreateTime] datetime NOT NULL,
    [LastActiveTime] datetime NOT NULL,
    [UserName] nvarchar(50) COLLATE NOCASE,
    [Domain] nvarchar(50) COLLATE NOCASE,
    [ClientName] nvarchar(50),
    [ClientAddress] nvarchar(50),
    [IsEnd] bit NOT NULL DEFAULT 0,
    [HasImage] bit NOT NULL DEFAULT 0
);
CREATE INDEX [IX_UserName_Domain] ON [SessionInfo] ([UserName], [Domain]);

CREATE TABLE [Snapshots] (
    [SessionId] guid NOT NULL,
    [SnapshotId] guid PRIMARY KEY NOT NULL,
	[BackgroundId] nvarchar(50),
    [SnapTime] datetime NOT NULL,
    [ProcessId] int,
    [ProcessName] nvarchar(50) COLLATE NOCASE,
    [WindowTitle] nvarchar(255) COLLATE NOCASE,
    [WindowUrl] nvarchar(2048) COLLATE NOCASE,
    [UrlHost] nvarchar(50) COLLATE NOCASE,
    [ControlText] nvarchar(1024) COLLATE NOCASE,
    [InputText] nvarchar(1024) COLLATE NOCASE
);
CREATE INDEX [IX_SnapTime] ON [Snapshots] ([SnapTime]);
CREATE INDEX [IX_SessionId] ON [Snapshots] ([SessionId]);
CREATE INDEX [IX_ProcessName] ON [Snapshots] ([ProcessName]);
CREATE INDEX [IX_UrlHost] ON [Snapshots] ([UrlHost]);

CREATE TABLE [SnapshotData] (
    [SessionId] guid NOT NULL,
    [SnapshotId] guid PRIMARY KEY NOT NULL,
    [BackgroundId] guid,
    [ScreenWidth] int NOT NULL,
    [ScreenHeight] int NOT NULL,
    [WindowHandle] int NOT NULL,
    [WindowRect] nvarchar(50) NOT NULL,
    [ImagePos] integer NOT NULL,
    [ImageLen] int NOT NULL,
    [MouseState] nvarchar(50),
    [IsGrayScale] bit NOT NULL,
    [EventsCount] int NOT NULL
);

/*-----------------------------------------------------------------------------
	View
-----------------------------------------------------------------------------*/
GO
CREATE VIEW SnapshotView
AS
SELECT   SS.SessionId, SS.SnapshotId, SS.BackgroundId, SS.SnapTime, SS.ProcessId, SS.ProcessName, SS.WindowTitle, 
                SS.WindowUrl, SS.UrlHost, SS.ControlText, SS.InputText, SD.WindowHandle, SD.WindowRect, SD.ImagePos, 
                SD.ImageLen, SD.MouseState, SD.IsGrayScale, SD.EventsCount
FROM      Snapshots SS LEFT OUTER JOIN
                SnapshotData SD ON SD.SnapshotId = SS.SnapshotId

GO
CREATE VIEW SessionView
AS
SELECT   DATE (datetime(SI.CreateTime)) AS SessionDate, SI.SessionId, SI.UserName, SI.Domain, SI.ClientName, 
                SI.ClientAddress, SI.IsEnd, SI.CreateTime, IFNULL(MAX(SV.SnapTime), SI.CreateTime) AS LastActiveTime, 
                COUNT(SV.SnapshotId) AS SnapshotCount, IFNULL(MAX(SV.ImagePos) + SV.ImageLen, 0) AS DataLength
FROM      SessionInfo SI LEFT OUTER JOIN
                SnapshotView SV ON SI.SessionId = SV.SessionId
GROUP BY SI.SessionId

GO
CREATE VIEW SearchTitle
AS
SELECT  date(datetime(SI.CreateTime)) AS SessionDate, SI.SessionId, SI.Domain, SI.UserName, 
		SS.ProcessName, SS.WindowTitle, MIN(SS.SnapTime) AS StartTime, MAX(SS.SnapTime) AS EndTime, COUNT(SS.SnapshotId) AS SnapshotCount
FROM	SessionInfo SI INNER JOIN
			Snapshots SS ON SI.SessionId = SS.SessionId
WHERE	length(ltrim(WindowTitle)) > 0
GROUP BY SI.SessionId, ProcessName, WindowTitle

GO
CREATE VIEW SearchText
AS
SELECT  date(datetime(SI.CreateTime)) AS SessionDate, SI.SessionId, SI.Domain, SI.UserName, 
		SS.ProcessName, SS.WindowTitle, MIN(SS.SnapTime) AS StartTime, MAX(SS.SnapTime) AS EndTime, COUNT(SS.SnapshotId) AS SnapshotCount,
		group_concat(InputText, '') AS InputText, group_concat(ControlText, '') AS ControlText
FROM	SessionInfo SI INNER JOIN
			Snapshots SS ON SI.SessionId = SS.SessionId
WHERE   length(WindowTitle) > 0
--WHERE   length(ltrim(InputText)) > 0 OR length(ltrim(ControlText)) > 0
GROUP BY SI.SessionId, ProcessName, WindowTitle

GO
CREATE VIEW SearchUrl
AS
SELECT  date(datetime(SI.CreateTime)) AS SessionDate, SI.SessionId, SI.Domain, SI.UserName, 
		UrlHost, WindowUrl, MIN(SnapTime) AS StartTime, MAX(SnapTime) AS EndTime, COUNT(SnapshotId) AS SnapshotCount
FROM	SessionInfo SI INNER JOIN
			Snapshots SS ON SI.SessionId = SS.SessionId
WHERE	length(ltrim(UrlHost)) > 0 AND length(ltrim(WindowUrl)) > 0
GROUP BY SI.SessionId, UrlHost, WindowUrl

GO
CREATE VIEW StatisticView
AS
SELECT  SI.SessionId, SI.Domain, SI.UserName, SI.ClientName, SI.ClientAddress, SI.IsEnd, 
		SS.SnapshotId, SS.SnapTime, SS.ProcessName, SS.UrlHost
FROM	SessionInfo SI LEFT OUTER JOIN
			Snapshots SS ON SI.SessionId = SS.SessionId

GO
CREATE VIEW HoursView
AS
SELECT   SI.Domain, SI.UserName, date(datetime(SS.SnapTime)) AS SnapDate, strftime('%H', SS.SnapTime) AS SnapHour, 
                COUNT(SS.SnapshotId) AS SnapCount
FROM      SessionInfo SI INNER JOIN
                Snapshots SS ON SI.SessionId = SS.SessionId
GROUP BY SI.Domain, SI.UserName, SnapDate, SnapHour