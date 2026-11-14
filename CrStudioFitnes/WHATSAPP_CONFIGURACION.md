# WhatsApp Cloud API - CVStudioFitness

## Endpoint público

Configurar en Meta como Callback URL:

https://cvstudiofitness.com/api/whatsapp/webhook

El endpoint GET se usa para la verificación de Meta y el POST recibe:
- estados del mensaje: sent, delivered, read, failed;
- clic en Confirmar reserva;
- clic en Cancelar reserva.

## Plantillas necesarias

Crear y aprobar en WhatsApp Manager tres plantillas de categoría Utility.

### 1. recordatorio_reserva_confirmacion

Idioma configurado por defecto: es

Cuerpo con exactamente 3 variables:

Hola {{1}}.
Te recordamos tu reserva en CVStudioFitness para el {{2}} a las {{3}}.

¿Vas a asistir?

Botones Quick Reply, en este orden:
1. Sí, asistiré
2. Cancelar reserva

El sistema envía payloads internos:
- CONFIRMAR_<IdReserva>
- CANCELAR_<IdReserva>

### 2. recordatorio_reserva_proxima

Se envía aproximadamente 1 hora y 30 minutos antes de la reserva.

Cuerpo con exactamente 3 variables:

Hola {{1}}.
Tu reserva en CVStudioFitness es hoy {{2}} a las {{3}}.

¿Vas a asistir?

Botones Quick Reply, en este orden:
1. Sí, asistiré
2. Cancelar reserva

El sistema envía payloads internos:
- CONFIRMAR_<IdReserva>
- CANCELAR_<IdReserva>

La cancelación solo se acepta mientras falte más de 1 hora para la reserva.
Ejemplo: reserva 7:00 PM, aviso 5:30 PM, puede cancelar hasta antes de las 6:00 PM.

### 3. recordatorio_reservas_personal

Se envía a todos los usuarios con rol Administrador o Entrenador que tengan TelefonoPersonal.

Se envía 20 minutos antes de cada hora activa configurada en HorasReserva.

Cuerpo con exactamente 2 variables:

Reservas para las {{1}}:

{{2}}

Valores enviados:
- {{1}} = hora a atender, por ejemplo 06:00 PM.
- {{2}} = lista de reservas activas con formato Nombre - Cédula.

Ejemplo con reservas:

Reservas para las 06:00 PM:

María Pérez - 101110111
Juan Rodríguez - 202220222

Ejemplo sin reservas:

Reservas para las 06:00 PM:

No hay reservas

Este mensaje se envía aunque no existan reservas para la hora activa.

## Variables de configuración

No guardar tokens reales en GitHub. En el repositorio, Enabled debe permanecer en false y PhoneNumberId/AccessToken deben permanecer vacíos. Para desarrollo usar User Secrets; para producción usar variables de entorno del servidor.

Configurar en el servidor:

WhatsApp__Enabled=true
WhatsApp__GraphApiVersion=v25.0
WhatsApp__PhoneNumberId=<PHONE_NUMBER_ID>
WhatsApp__AccessToken=<TOKEN_PERMANENTE>
WhatsApp__TemplateNextDay=recordatorio_reserva_confirmacion
WhatsApp__TemplateNextHour=recordatorio_reserva_proxima
WhatsApp__TemplateStaffUpcoming=recordatorio_reservas_personal
WhatsApp__LanguageCode=es
WhatsApp__VerifyToken=<TOKEN_ELEGIDO_POR_USTED>
WhatsApp__AppSecret=<APP_SECRET_DE_META>

El worker necesita Enabled, GraphApiVersion, PhoneNumberId, AccessToken y las tres plantillas.
El sitio web necesita VerifyToken y AppSecret para validar el webhook, y PhoneNumberId/AccessToken para responder al usuario después de Confirmar/Cancelar.

Si la plantilla se creó con una variante regional de español, ajustar WhatsApp__LanguageCode al código exacto que muestre Meta.

## Desarrollo local

En EnviaCorreoNotificaciones usar Manage User Secrets / Administrar secretos de usuario:

{
  "WhatsApp": {
    "Enabled": true,
    "GraphApiVersion": "v25.0",
    "PhoneNumberId": "<PHONE_NUMBER_ID>",
    "AccessToken": "<ACCESS_TOKEN>",
    "TemplateNextDay": "recordatorio_reserva_confirmacion",
    "TemplateNextHour": "recordatorio_reserva_proxima",
    "TemplateStaffUpcoming": "recordatorio_reservas_personal",
    "LanguageCode": "es"
  }
}

Para probar únicamente el envío desde localhost no se necesita webhook, VerifyToken ni AppSecret.

Los logs del Worker se guardan por defecto en la carpeta logs junto al ejecutable. Se puede cambiar la carpeta definiendo la variable de entorno CRSTUDIO_LOG_DIR.

## Suscripción del webhook

En la aplicación de Meta:
1. Añadir el producto WhatsApp.
2. Configurar Webhooks.
3. Callback URL: https://cvstudiofitness.com/api/whatsapp/webhook
4. Verify token: el mismo valor configurado como WhatsApp__VerifyToken.
5. Suscribir el campo messages de la cuenta de WhatsApp Business.

## Base de datos

La migración 20261106203000_WhatsAppReservas crea ReservaWhatsAppReminder.

La migración 20261114220000_StaffWhatsAppReminder crea StaffWhatsAppReminder para evitar mensajes duplicados al personal por fecha, hora y destinatario.

Ejecutar en el proyecto CrStudioFitnes:

Update-Database

## Reglas implementadas

- Solo se envían recordatorios de clientes para Reservas.Activa = true.
- Una reserva cancelada no recibe recordatorios posteriores.
- Día anterior: WhatsApp con Sí, asistiré / Cancelar reserva.
- 1 hora y 30 minutos antes: WhatsApp con Sí, asistiré / Cancelar reserva.
- Si el usuario no responde, la reserva permanece activa.
- Confirmar mantiene la reserva activa y no altera las lecciones.
- Cancelar solo se permite mientras falte más de 1 hora.
- Al cancelar correctamente: Reserva.Activa=false, se devuelve 1 lección y se registra en movimientosreservas.
- A 1 hora o menos de la reserva, Cancelar es rechazado y la reserva permanece activa.
- Personal Administrador/Entrenador: aviso 20 minutos antes de cada hora activa.
- El aviso al personal siempre se envía: con lista Nombre - Cédula o con el texto No hay reservas.
- El tracking de personal evita duplicados y permite reintentos si el primer envío falla.
