namespace SQ7MRU.Utils
{
    /// <summary>
    /// Class contains QSO Fields v3.0.6 ADIF
    /// http://www.adif.org/306/ADIF_306.htm#QSO_Fields
    /// </summary>
    public class AdifRow
    {
        ///Field Name : ADDRESS
        ///Data Type : MultilineString
        ///Description : the contacted station's complete mailing address: full name, street address, city, postal code, and country
        public string ADDRESS { get; set; }

        ///Field Name : ADDRESS_INTL
        ///Data Type : IntlMultilineString
        ///Description : the contacted station's complete mailing address: full name, street address, city, postal code, and country
        public string ADDRESS_INTL { get; set; }

        ///Field Name : AGE
        ///Data Type : Number
        ///Description : the contacted station's operator's age in years in the range 0 to 120 (inclusive)
        public string AGE { get; set; }

        ///Field Name : A_INDEX
        ///Data Type : Number
        ///Description : the geomagnetic A index at the time of the QSO in the range 0 to 400 (inclusive)
        public string A_INDEX { get; set; }

        ///Field Name : ANT_AZ
        ///Data Type : Number
        ///Description : the logging station's antenna azimuth, in degrees with a value between 0 to 360 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 370 is exported as 10). True north is 0 degrees with values increasing in a clockwise direction.
        public string ANT_AZ { get; set; }

        ///Field Name : ANT_EL
        ///Data Type : Number
        ///Description : the logging station's antenna elevation, in degrees with a value between -90 to 90 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 100 is exported as 80). The horizon is 0 degrees with values increasing as the angle moves in an upward direction.
        public string ANT_EL { get; set; }

        ///Field Name : ANT_PATH
        ///Data Type : Enumeration
        ///Enumeration : Ant Path
        ///Description : the signal path
        public string ANT_PATH { get; set; }

        ///Field Name : ARRL_SECT
        ///Data Type : Enumeration
        ///Enumeration : ARRL Section
        ///Description : the contacted station's ARRL section
        public string ARRL_SECT { get; set; }

        ///Field Name : AWARD_SUBMITTED
        ///Data Type : SponsoredAwardList
        ///Enumeration : Sponsored Award
        ///Description : the list of awards submitted to a sponsor.  note that this field might not be used in a QSO record. It might be used to convey information about a user's "Award Account" between an award sponsor and the user. For example, AA6YQ might submit a request for awards by sending the following:  <CALL:5>AA6YQ <AWARD_SUBMITTED:64>ADIF_CENTURY_BASIC,ADIF_CENTURY_SILVER, ADIF_SPECTRUM_100-160m
        public string AWARD_SUBMITTED { get; set; }

        ///Field Name : AWARD_GRANTED
        ///Data Type : SponsoredAwardList
        ///Enumeration : Sponsored Award
        ///Description : the list of awards granted by a sponsor.  note that this field might not be used in a QSO record. It might be used to convey information about a user's "Award Account" between an award sponsor and the user. For example, in response to a request "send me a list of the awards granted to AA6YQ", this might be received:  <CALL:5>AA6YQ <AWARD_GRANTED:64>ADIF_CENTURY_BASIC,ADIF_CENTURY_SILVER, ADIF_SPECTRUM_100-160m
        public string AWARD_GRANTED { get; set; }

        ///Field Name : BAND
        ///Data Type : Enumeration
        ///Enumeration : Band
        ///Description : QSO Band
        public string BAND { get; set; }

        ///Field Name : BAND_RX
        ///Data Type : Enumeration
        ///Enumeration : Band
        ///Description : in a split frequency QSO, the logging station's receiving band
        public string BAND_RX { get; set; }

        ///Field Name : CALL
        ///Data Type : String
        ///Description : the contacted station's Callsign
        public string CALL { get; set; }

        ///Field Name : CHECK
        ///Data Type : String
        ///Description : contest check (e.g. for ARRL Sweepstakes)
        public string CHECK { get; set; }

        ///Field Name : CLASS
        ///Data Type : String
        ///Description : contest class (e.g. for ARRL Field Day)
        public string CLASS { get; set; }

        ///Field Name : CLUBLOG_QSO_UPLOAD_DATE
        ///Data Type : Date
        ///Description : the date the QSO was last uploaded to the Club Log online service
        public string CLUBLOG_QSO_UPLOAD_DATE { get; set; }

        ///Field Name : CLUBLOG_QSO_UPLOAD_STATUS
        ///Data Type : Enumeration
        ///Enumeration : QSO Upload Status
        ///Description : the upload status of the QSO on the Club Log online service
        public string CLUBLOG_QSO_UPLOAD_STATUS { get; set; }

        ///Field Name : CNTY
        ///Data Type : Enumeration
        ///Enumeration : (Secondary Administrative Subdivision, function of DXCC field's value)
        ///Description : the contacted station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format
        public string CNTY { get; set; }

        ///Field Name : COMMENT
        ///Data Type : String
        ///Description : comment field for QSO
        public string COMMENT { get; set; }

        ///Field Name : COMMENT_INTL
        ///Data Type : IntlString
        ///Description : comment field for QSO
        public string COMMENT_INTL { get; set; }

        ///Field Name : CONT
        ///Data Type : Enumeration
        ///Enumeration : Continent
        ///Description : the contacted station's Continent
        public string CONT { get; set; }

        ///Field Name : CONTACTED_OP
        ///Data Type : String
        ///Description : the callsign of the individual operating the contacted station
        public string CONTACTED_OP { get; set; }

        ///Field Name : CONTEST_ID
        ///Data Type : String
        ///Enumeration : Contest ID
        ///Description : QSO Contest Identifieruse enumeration values for interoperability
        public string CONTEST_ID { get; set; }

        ///Field Name : COUNTRY
        ///Data Type : String
        ///Description : the contacted station's DXCC entity name
        public string COUNTRY { get; set; }

        ///Field Name : COUNTRY_INTL
        ///Data Type : IntlString
        ///Description : the contacted station's DXCC entity name
        public string COUNTRY_INTL { get; set; }

        ///Field Name : CQZ
        ///Data Type : PositiveInteger
        ///Description : the contacted station's CQ Zone in the range 1 to 40 (inclusive)
        public string CQZ { get; set; }

        ///Field Name : CREDIT_SUBMITTED
        ///Data Type : CreditList  AwardList (import-only)
        ///Enumeration : Credit  Award (import-only)
        ///Description : the list of credits sought for this QSO  Use of data type AwardList and enumeration Award are import-only
        public string CREDIT_SUBMITTED { get; set; }

        ///Field Name : CREDIT_GRANTED
        ///Data Type : CreditList  AwardList (import-only)
        ///Enumeration : Credit  Award (import-only)
        ///Description : the list of credits granted to this QSO  Use of data type AwardList and enumeration Award are import-only
        public string CREDIT_GRANTED { get; set; }

        ///Field Name : DARC_DOK
        ///Data Type : Enumeration
        ///Enumeration : (DOKs listed in DARC DOK List  Special DOKs listed in DARC Special DOK List)
        ///Description : the contacted station's DARC DOK (District Location Code)  A DOK comprises letters and numbers, e.g. <DARC_DOK:3>A01
        public string DARC_DOK { get; set; }

        ///Field Name : DISTANCE
        ///Data Type : Number
        ///Description : the distance between the logging station and the contacted station in kilometers via the specified signal path with a value greater than or equal to 0
        public string DISTANCE { get; set; }

        ///Field Name : DXCC
        ///Data Type : Enumeration
        ///Enumeration : DXCC Entity Code
        ///Description : the contacted station's DXCC Entity Code  <DXCC:1>0 means that the contacted station is known not to be within a DXCC entity.
        public string DXCC { get; set; }

        ///Field Name : EMAIL
        ///Data Type : String
        ///Description : the contacted station's email address
        public string EMAIL { get; set; }

        ///Field Name : EQ_CALL
        ///Data Type : String
        ///Description : the contacted station's owner's callsign
        public string EQ_CALL { get; set; }

        ///Field Name : EQSL_QSLRDATE
        ///Data Type : Date
        ///Description : date QSL received from eQSL.cc(only valid if EQSL_QSL_RCVD is Y, I, or V) (V import-only)
        public string EQSL_QSLRDATE { get; set; }

        ///Field Name : EQSL_QSLSDATE
        ///Data Type : Date
        ///Description : date QSL sent to eQSL.cc(only valid if EQSL_QSL_SENT is Y, Q, or I)
        public string EQSL_QSLSDATE { get; set; }

        ///Field Name : EQSL_QSL_RCVD
        ///Data Type : Enumeration
        ///Enumeration : QSL Rcvd
        ///Description : eQSL.cc QSL received status  instead of V (import-only) use <CREDIT_GRANTED:42>CQWAZ:eqsl,CQWAZ_BAND:eqsl,CQWAZ_MODE:eqsl  Default Value: N
        public string EQSL_QSL_RCVD { get; set; }

        ///Field Name : EQSL_QSL_SENT
        ///Data Type : Enumeration
        ///Enumeration : QSL Sent
        ///Description : eQSL.cc QSL sent status  Default Value: N
        public string EQSL_QSL_SENT { get; set; }

        ///Field Name : FISTS
        ///Data Type : PositiveInteger
        ///Description : the contacted station's FISTS CW Club member number with a value greater than 0.
        public string FISTS { get; set; }

        ///Field Name : FISTS_CC
        ///Data Type : PositiveInteger
        ///Description : the contacted station's FISTS CW Club Century Certificate (CC) number with a value greater than 0.
        public string FISTS_CC { get; set; }

        ///Field Name : FORCE_INIT
        ///Data Type : Boolean
        ///Description : new EME "initial"
        public string FORCE_INIT { get; set; }

        ///Field Name : FREQ
        ///Data Type : Number
        ///Description : QSO frequency in Megahertz
        public string FREQ { get; set; }

        ///Field Name : FREQ_RX
        ///Data Type : Number
        ///Description : in a split frequency QSO, the logging station's receiving frequency in Megahertz
        public string FREQ_RX { get; set; }

        ///Field Name : GRIDSQUARE
        ///Data Type : GridSquare
        ///Description : the contacted station's 2-character, 4-character, 6-character, or 8-character Maidenhead Grid Square
        public string GRIDSQUARE { get; set; }

        ///Field Name : GUEST_OP
        ///Data Type : String
        ///Description : import-only: use OPERATOR instead
        public string GUEST_OP { get; set; }

        ///Field Name : HRDLOG_QSO_UPLOAD_DATE
        ///Data Type : Date
        ///Description : the date the QSO was last uploaded to the HRDLog.net online service
        public string HRDLOG_QSO_UPLOAD_DATE { get; set; }

        ///Field Name : HRDLOG_QSO_UPLOAD_STATUS
        ///Data Type : Enumeration
        ///Enumeration : QSO Upload Status
        ///Description : the upload status of the QSO on the HRDLog.net online service
        public string HRDLOG_QSO_UPLOAD_STATUS { get; set; }

        ///Field Name : IOTA
        ///Data Type : IOTARefNo
        ///Description : the contacted station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 <= XXX <= 999 [use leading zeroes]
        public string IOTA { get; set; }

        ///Field Name : IOTA_ISLAND_ID
        ///Data Type : PositiveInteger
        ///Description : the contacted station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional]
        public string IOTA_ISLAND_ID { get; set; }

        ///Field Name : ITUZ
        ///Data Type : PositiveInteger
        ///Description : the contacted station's ITU zone in the range 1 to 90 (inclusive)
        public string ITUZ { get; set; }

        ///Field Name : K_INDEX
        ///Data Type : Integer
        ///Description : the geomagnetic K index at the time of the QSO in the range 0 to 9 (inclusive)
        public string K_INDEX { get; set; }

        ///Field Name : LAT
        ///Data Type : Location
        ///Description : the contacted station's latitude
        public string LAT { get; set; }

        ///Field Name : LON
        ///Data Type : Location
        ///Description : the contacted station's longitude
        public string LON { get; set; }

        ///Field Name : LOTW_QSLRDATE
        ///Data Type : Date
        ///Description : date QSL received from ARRL Logbook of the World(only valid if LOTW_QSL_RCVD is Y, I, or V) (V import-only)
        public string LOTW_QSLRDATE { get; set; }

        ///Field Name : LOTW_QSLSDATE
        ///Data Type : Date
        ///Description : date QSL sent to ARRL Logbook of the World(only valid if LOTW_QSL_SENT is Y, Q, or I)
        public string LOTW_QSLSDATE { get; set; }

        ///Field Name : LOTW_QSL_RCVD
        ///Data Type : Enumeration
        ///Enumeration : QSL Rcvd
        ///Description : ARRL Logbook of the World QSL received status  instead of V (import-only) use <CREDIT_GRANTED:39>DXCC:lotw,DXCC_BAND:lotw,DXCC_MODE:lotw  Default Value: N
        public string LOTW_QSL_RCVD { get; set; }

        ///Field Name : LOTW_QSL_SENT
        ///Data Type : Enumeration
        ///Enumeration : QSL Sent
        ///Description : ARRL Logbook of the World QSL sent status  Default Value: N
        public string LOTW_QSL_SENT { get; set; }

        ///Field Name : MAX_BURSTS
        ///Data Type : Number
        ///Description : maximum length of meteor scatter bursts heard by the logging station, in seconds with a value greater than or equal to 0
        public string MAX_BURSTS { get; set; }

        ///Field Name : MODE
        ///Data Type : Enumeration
        ///Enumeration : Mode
        ///Description : QSO Mode
        public string MODE { get; set; }

        ///Field Name : MS_SHOWER
        ///Data Type : String
        ///Description : For Meteor Scatter QSOs, the name of the meteor shower in progress
        public string MS_SHOWER { get; set; }

        ///Field Name : MY_ANTENNA
        ///Data Type : String
        ///Description : the logging station's antenna
        public string MY_ANTENNA { get; set; }

        ///Field Name : MY_ANTENNA_INTL
        ///Data Type : IntlString
        ///Description : the logging station's antenna
        public string MY_ANTENNA_INTL { get; set; }

        ///Field Name : MY_CITY
        ///Data Type : String
        ///Description : the logging station's city
        public string MY_CITY { get; set; }

        ///Field Name : MY_CITY_INTL
        ///Data Type : IntlString
        ///Description : the logging station's city
        public string MY_CITY_INTL { get; set; }

        ///Field Name : MY_CNTY
        ///Data Type : Enumeration
        ///Enumeration : (Secondary Administrative Subdivision, function of MY_DXCC field's value)
        ///Description : the logging station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format
        public string MY_CNTY { get; set; }

        ///Field Name : MY_COUNTRY
        ///Data Type : String
        ///Enumeration : Country
        ///Description : the logging station's DXCC entity name
        public string MY_COUNTRY { get; set; }

        ///Field Name : MY_COUNTRY_INTL
        ///Data Type : IntlString
        ///Enumeration : Country
        ///Description : the logging station's DXCC entity name
        public string MY_COUNTRY_INTL { get; set; }

        ///Field Name : MY_CQ_ZONE
        ///Data Type : PositiveInteger
        ///Description : the logging station's CQ Zone in the range 1 to 40 (inclusive)
        public string MY_CQ_ZONE { get; set; }

        ///Field Name : MY_DXCC
        ///Data Type : Enumeration
        ///Enumeration : DXCC Entity Code
        ///Description : the logging station's DXCC Entity Code  <MY_DXCC:1>0 means that the logging station is known not to be within a DXCC entity.
        public string MY_DXCC { get; set; }

        ///Field Name : MY_FISTS
        ///Data Type : PositiveInteger
        ///Description : the logging station's FISTS CW Club member number with a value greater than 0.
        public string MY_FISTS { get; set; }

        ///Field Name : MY_GRIDSQUARE
        ///Data Type : GridSquare
        ///Description : the logging station's 2-character, 4-character, 6-character, or 8-character Maidenhead Grid Square
        public string MY_GRIDSQUARE { get; set; }

        ///Field Name : MY_IOTA
        ///Data Type : IOTARefNo
        ///Description : the logging station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 <= XXX <= 999 [use leading zeroes]
        public string MY_IOTA { get; set; }

        ///Field Name : MY_IOTA_ISLAND_ID
        ///Data Type : PositiveInteger
        ///Description : the logging station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional]
        public string MY_IOTA_ISLAND_ID { get; set; }

        ///Field Name : MY_ITU_ZONE
        ///Data Type : PositiveInteger
        ///Description : the logging station's ITU zone in the range 1 to 90 (inclusive)
        public string MY_ITU_ZONE { get; set; }

        ///Field Name : MY_LAT
        ///Data Type : Location
        ///Description : the logging station's latitude
        public string MY_LAT { get; set; }

        ///Field Name : MY_LON
        ///Data Type : Location
        ///Description : the logging station's longitude
        public string MY_LON { get; set; }

        ///Field Name : MY_NAME
        ///Data Type : String
        ///Description : the logging operator's name
        public string MY_NAME { get; set; }

        ///Field Name : MY_NAME_INTL
        ///Data Type : IntlString
        ///Description : the logging operator's name
        public string MY_NAME_INTL { get; set; }

        ///Field Name : MY_POSTAL_CODE
        ///Data Type : String
        ///Description : the logging station's postal code
        public string MY_POSTAL_CODE { get; set; }

        ///Field Name : MY_POSTAL_CODE_INTL
        ///Data Type : IntlString
        ///Description : the logging station's postal code
        public string MY_POSTAL_CODE_INTL { get; set; }

        ///Field Name : MY_RIG
        ///Data Type : String
        ///Description : description of the logging station's equipment
        public string MY_RIG { get; set; }

        ///Field Name : MY_RIG_INTL
        ///Data Type : IntlString
        ///Description : description of the logging station's equipment
        public string MY_RIG_INTL { get; set; }

        ///Field Name : MY_SIG
        ///Data Type : String
        ///Description : special interest activity or event
        public string MY_SIG { get; set; }

        ///Field Name : MY_SIG_INTL
        ///Data Type : IntlString
        ///Description : special interest activity or event
        public string MY_SIG_INTL { get; set; }

        ///Field Name : MY_SIG_INFO
        ///Data Type : String
        ///Description : special interest activity or event information
        public string MY_SIG_INFO { get; set; }

        ///Field Name : MY_SIG_INFO_INTL
        ///Data Type : IntlString
        ///Description : special interest activity or event information
        public string MY_SIG_INFO_INTL { get; set; }

        ///Field Name : MY_SOTA_REF
        ///Data Type : SOTARef
        ///Description : the logging station's International SOTA Reference.
        public string MY_SOTA_REF { get; set; }

        ///Field Name : MY_STATE
        ///Data Type : Enumeration
        ///Enumeration : (Primary Administrative Subdivision, function of MY_DXCC field's value)
        ///Description : the code for the logging station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province)
        public string MY_STATE { get; set; }

        ///Field Name : MY_STREET
        ///Data Type : String
        ///Description : the logging station's street
        public string MY_STREET { get; set; }

        ///Field Name : MY_STREET_INTL
        ///Data Type : IntlString
        ///Description : the logging station's street
        public string MY_STREET_INTL { get; set; }

        ///Field Name : MY_USACA_COUNTIES
        ///Data Type : SecondarySubdivisionList
        ///Description : two US counties in the case where the logging station is located on a border between two counties, representing counties that the contacted station may claim for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire
        public string MY_USACA_COUNTIES { get; set; }

        ///Field Name : MY_VUCC_GRIDS
        ///Data Type : GridSquareList
        ///Description : two or four adjacent Maidenhead grid locators, each four characters long, representing the logging station's grid squares that the contacted station may claim for the ARRL VUCC award program. E.g.  EN98,FM08,EM97,FM07
        public string MY_VUCC_GRIDS { get; set; }

        ///Field Name : NAME
        ///Data Type : String
        ///Description : the contacted station's operator's name
        public string NAME { get; set; }

        ///Field Name : NAME_INTL
        ///Data Type : IntlString
        ///Description : the contacted station's operator's name
        public string NAME_INTL { get; set; }

        ///Field Name : NOTES
        ///Data Type : MultilineString
        ///Description : QSO notes
        public string NOTES { get; set; }

        ///Field Name : NOTES_INTL
        ///Data Type : IntlMultilineString
        ///Description : QSO notes
        public string NOTES_INTL { get; set; }

        ///Field Name : NR_BURSTS
        ///Data Type : Integer
        ///Description : the number of meteor scatter bursts heard by the logging station with a value greater than or equal to 0
        public string NR_BURSTS { get; set; }

        ///Field Name : NR_PINGS
        ///Data Type : Integer
        ///Description : the number of meteor scatter pings heard by the logging station with a value greater than or equal to 0
        public string NR_PINGS { get; set; }

        ///Field Name : OPERATOR
        ///Data Type : String
        ///Description : the logging operator's callsignif STATION_CALLSIGN is absent, OPERATOR shall be treated as both the logging station's callsign and the logging operator's callsign
        public string OPERATOR { get; set; }

        ///Field Name : OWNER_CALLSIGN
        ///Data Type : String
        ///Description : the callsign of the owner of the station used to log the contact (the callsign of the OPERATOR's host)if OWNER_CALLSIGN is absent, STATION_CALLSIGN shall be treated as both the logging station's callsign and the callsign of the owner of the station
        public string OWNER_CALLSIGN { get; set; }

        ///Field Name : PFX
        ///Data Type : String
        ///Description : the contacted station's WPX prefix
        public string PFX { get; set; }

        ///Field Name : PRECEDENCE
        ///Data Type : String
        ///Description : contest precedence (e.g. for ARRL Sweepstakes)
        public string PRECEDENCE { get; set; }

        ///Field Name : PROP_MODE
        ///Data Type : Enumeration
        ///Enumeration : Propagation Mode
        ///Description : QSO propagation mode
        public string PROP_MODE { get; set; }

        ///Field Name : PUBLIC_KEY
        ///Data Type : String
        ///Description : public encryption key
        public string PUBLIC_KEY { get; set; }

        ///Field Name : QRZCOM_QSO_UPLOAD_DATE
        ///Data Type : Date
        ///Description : the date the QSO was last uploaded to the QRZ.COM online service
        public string QRZCOM_QSO_UPLOAD_DATE { get; set; }

        ///Field Name : QRZCOM_QSO_UPLOAD_STATUS
        ///Data Type : Enumeration
        ///Enumeration : QSO Upload Status
        ///Description : the upload status of the QSO on the QRZ.COM online service
        public string QRZCOM_QSO_UPLOAD_STATUS { get; set; }

        ///Field Name : QSLMSG
        ///Data Type : MultilineString
        ///Description : QSL card message
        public string QSLMSG { get; set; }

        ///Field Name : QSLMSG_INTL
        ///Data Type : IntlMultilineString
        ///Description : QSL card message
        public string QSLMSG_INTL { get; set; }

        ///Field Name : QSLRDATE
        ///Data Type : Date
        ///Description : QSL received date(only valid if QSL_RCVD is Y, I, or V) (V import-only)
        public string QSLRDATE { get; set; }

        ///Field Name : QSLSDATE
        ///Data Type : Date
        ///Description : QSL sent date(only valid if QSL_SENT is Y, Q, or I)
        public string QSLSDATE { get; set; }

        ///Field Name : QSL_RCVD
        ///Data Type : Enumeration
        ///Enumeration : QSL Rcvd
        ///Description : QSL received status  instead of V (import-only) use <CREDIT_GRANTED:39>DXCC:card,DXCC_BAND:card,DXCC_MODE:card  Default Value: N
        public string QSL_RCVD { get; set; }

        ///Field Name : QSL_RCVD_VIA
        ///Data Type : Enumeration
        ///Enumeration : QSL Via
        ///Description : means by which the QSL was received by the logging station  use of M (manager) is import-only
        public string QSL_RCVD_VIA { get; set; }

        ///Field Name : QSL_SENT
        ///Data Type : Enumeration
        ///Enumeration : QSL Sent
        ///Description : QSL sent status  Default Value: N
        public string QSL_SENT { get; set; }

        ///Field Name : QSL_SENT_VIA
        ///Data Type : Enumeration
        ///Enumeration : QSL Via
        ///Description : means by which the QSL was sent by the logging station  use of M (manager) is import-only
        public string QSL_SENT_VIA { get; set; }

        ///Field Name : QSL_VIA
        ///Data Type : String
        ///Description : the contacted station's QSL route
        public string QSL_VIA { get; set; }

        ///Field Name : QSO_COMPLETE
        ///Data Type : Enumeration
        ///Enumeration : QSO Complete
        ///Description : indicates whether the QSO was complete from the perspective of the logging stationY - yes N - no NIL - not heard ? - uncertain
        public string QSO_COMPLETE { get; set; }

        ///Field Name : QSO_DATE
        ///Data Type : Date
        ///Description : date on which the QSO started
        public string QSO_DATE { get; set; }

        ///Field Name : QSO_DATE_OFF
        ///Data Type : Date
        ///Description : date on which the QSO ended
        public string QSO_DATE_OFF { get; set; }

        ///Field Name : QSO_RANDOM
        ///Data Type : Boolean
        ///Description : indicates whether the QSO was random or scheduled
        public string QSO_RANDOM { get; set; }

        ///Field Name : QTH
        ///Data Type : String
        ///Description : the contacted station's city
        public string QTH { get; set; }

        ///Field Name : QTH_INTL
        ///Data Type : IntlString
        ///Description : the contacted station's city
        public string QTH_INTL { get; set; }

        ///Field Name : REGION
        ///Data Type : Enumeration
        ///Enumeration : Region
        ///Description : the contacted station's WAE or CQ entity contained within a DXCC entity.the value None indicates that the WAE or CQ entity is the DXCC entity in the DXCC field. nothing can be inferred from the absence of the REGION field
        public string REGION { get; set; }

        ///Field Name : RIG
        ///Data Type : MultilineString
        ///Description : description of the contacted station's equipment
        public string RIG { get; set; }

        ///Field Name : RIG_INTL
        ///Data Type : IntlMultilineString
        ///Description : description of the contacted station's equipment
        public string RIG_INTL { get; set; }

        ///Field Name : RST_RCVD
        ///Data Type : String
        ///Description : signal report from the contacted station
        public string RST_RCVD { get; set; }

        ///Field Name : RST_SENT
        ///Data Type : String
        ///Description : signal report sent to the contacted station
        public string RST_SENT { get; set; }

        ///Field Name : RX_PWR
        ///Data Type : Number
        ///Description : the contacted station's transmitter power in Watts with a value greater than 0
        public string RX_PWR { get; set; }

        ///Field Name : SAT_MODE
        ///Data Type : String
        ///Description : satellite mode
        public string SAT_MODE { get; set; }

        ///Field Name : SAT_NAME
        ///Data Type : String
        ///Description : name of satellite
        public string SAT_NAME { get; set; }

        ///Field Name : SFI
        ///Data Type : Integer
        ///Description : the solar flux at the time of the QSO in the range 0 to 300 (inclusive).
        public string SFI { get; set; }

        ///Field Name : SIG
        ///Data Type : String
        ///Description : the name of the contacted station's special activity or interest group
        public string SIG { get; set; }

        ///Field Name : SIG_INTL
        ///Data Type : IntlString
        ///Description : the name of the contacted station's special activity or interest group
        public string SIG_INTL { get; set; }

        ///Field Name : SIG_INFO
        ///Data Type : String
        ///Description : information associated with the contacted station's activity or interest group
        public string SIG_INFO { get; set; }

        ///Field Name : SIG_INFO_INTL
        ///Data Type : IntlString
        ///Description : information associated with the contacted station's activity or interest group
        public string SIG_INFO_INTL { get; set; }

        ///Field Name : SILENT_KEY
        ///Data Type : Boolean
        ///Description : Y' indicates that the contacted station's operator is now a Silent Key.
        public string SILENT_KEY { get; set; }

        ///Field Name : SKCC
        ///Data Type : String
        ///Description : the contacted station's Straight Key Century Club (SKCC) member information
        public string SKCC { get; set; }

        ///Field Name : SOTA_REF
        ///Data Type : SOTARef
        ///Description : the contacted station's International SOTA Reference.
        public string SOTA_REF { get; set; }

        ///Field Name : SRX
        ///Data Type : Integer
        ///Description : contest QSO received serial number with a value greater than or equal to 0
        public string SRX { get; set; }

        ///Field Name : SRX_STRING
        ///Data Type : String
        ///Description : contest QSO received informationuse Cabrillo format to convey contest information for which ADIF fields are not specified in the event of a conflict between information in a dedicated contest field and this field, information in the dedicated contest field shall prevail
        public string SRX_STRING { get; set; }

        ///Field Name : STATE
        ///Data Type : Enumeration
        ///Enumeration : (Primary Administrative Subdivision, function of DXCC field's value)
        ///Description : the code for the contacted station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province)
        public string STATE { get; set; }

        ///Field Name : STATION_CALLSIGN
        ///Data Type : String
        ///Description : the logging station's callsign (the callsign used over the air)if STATION_CALLSIGN is absent, OPERATOR shall be treated as both the logging station's callsign and the logging operator's callsign
        public string STATION_CALLSIGN { get; set; }

        ///Field Name : STX
        ///Data Type : Integer
        ///Description : contest QSO transmitted serial number with a value greater than or equal to 0
        public string STX { get; set; }

        ///Field Name : STX_STRING
        ///Data Type : String
        ///Description : contest QSO transmitted informationuse Cabrillo format to convey contest information for which ADIF fields are not specified in the event of a conflict between information in a dedicated contest field and this field, information in the dedicated contest field shall prevail
        public string STX_STRING { get; set; }

        ///Field Name : SUBMODE
        ///Data Type : String
        ///Enumeration : Submode (function of MODE field's value)
        ///Description : QSO Submode  use enumeration values for interoperability
        public string SUBMODE { get; set; }

        ///Field Name : SWL
        ///Data Type : Boolean
        ///Description : indicates that the QSO information pertains to an SWL report
        public string SWL { get; set; }

        ///Field Name : TEN_TEN
        ///Data Type : PositiveInteger
        ///Description : Ten-Ten number with a value greater than 0
        public string TEN_TEN { get; set; }

        ///Field Name : TIME_OFF
        ///Data Type : Time
        ///Description : HHMM or HHMMSS in UTCin the absence of <QSO_DATE_OFF>, the QSO duration is less than 24 hours
        public string TIME_OFF { get; set; }

        ///Field Name : TIME_ON
        ///Data Type : Time
        ///Description : HHMM or HHMMSS in UTC
        public string TIME_ON { get; set; }

        ///Field Name : TX_PWR
        ///Data Type : Number
        ///Description : the logging station's power in Watts with a value greater than 0
        public string TX_PWR { get; set; }

        ///Field Name : UKSMG
        ///Data Type : PositiveInteger
        ///Description : the contacted station's UKSMG member number with a value greater than 0
        public string UKSMG { get; set; }

        ///Field Name : USACA_COUNTIES
        ///Data Type : SecondarySubdivisionList
        ///Description : two US counties in the case where the contacted station is located on a border between two counties, representing counties credited to the QSO for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire
        public string USACA_COUNTIES { get; set; }

        ///Field Name : VE_PROV
        ///Data Type : String
        ///Description : import-only: use STATE instead
        public string VE_PROV { get; set; }

        ///Field Name : VUCC_GRIDS
        ///Data Type : GridSquareList
        ///Description : two or four adjacent Maidenhead grid locators, each four characters long, representing the contacted station's grid squares credited to the QSO for the ARRL VUCC award program. E.g.  EN98,FM08,EM97,FM07
        public string VUCC_GRIDS { get; set; }

        ///Field Name : WEB
        ///Data Type : String
        ///Description : the contacted station's URL
        public string WEB { get; set; }
    }
}