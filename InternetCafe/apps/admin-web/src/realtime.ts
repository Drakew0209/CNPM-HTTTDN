import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
export function connectRealtime(token: string, refresh: () => void) {
  const url = import.meta.env.VITE_SIGNALR_URL;
  if (!url) return () => {};
  const connection = new HubConnectionBuilder().withUrl(url, { accessTokenFactory: () => token }).withAutomaticReconnect([0, 2000, 10000, 30000]).configureLogging(LogLevel.Warning).build();
  for (const event of ['ComputerChanged', 'SessionStarted', 'SessionEnded', 'BalanceChanged', 'TopupRequested', 'TopupDecided', 'OrderCreated', 'OrderChanged']) connection.on(event, refresh);
  connection.onreconnected(refresh);
  void connection.start().then(refresh).catch(() => { /* HTTP polling remains active until hub is configured. */ });
  return () => { void connection.stop(); };
}
