class ULog extends Object	
	dllbind(managed_logger);

dllimport final function Assert(string condition, string message, string channels, string source, string className, string stateName, string funcName);
dllimport final function Debug(string message, string channels, string source, string className, string stateName, string funcName);
dllimport final function Info(string message, string channels, string source, string className, string stateName, string funcName);
dllimport final function Warn(string message, string channels, string source, string className, string stateName, string funcName);
dllimport final function Error(string message, string channels, string source, string className, string stateName, string funcName);
dllimport final function Fatal(string message, string channels, string source, string className, string stateName, string funcName);

static function StaticAssert( string condition, coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Assert(condition, message, channels, source, className, stateName, funcName);
}

static function StaticDebug( coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Debug(message, channels, source, className, stateName, funcName);
}

static function StaticInfo( coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Info(message, channels, source, className, stateName, funcName);
}

static function StaticWarn( coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Warn(message, channels, source, className, stateName, funcName);
}

static function StaticError( coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Error(message, channels, source, className, stateName, funcName);
}

static function StaticFatal( coerce string message, string channels, string source, string className, string stateName, string funcName )
{
	local ULog log;
	log = `NEW( ULog );

	log.Fatal(message, channels, source, className, stateName, funcName);
}

DefaultProperties
{
}
