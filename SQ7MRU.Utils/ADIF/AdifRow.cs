#nullable disable
namespace SQ7MRU.Utils
{
    /// <summary>
    /// Represents the implemented legacy ADIF QSO field model using optional raw strings.
    /// </summary>
    /// <seealso href="https://www.adif.org/100/adif_100.htm">ADIF 1.0 base field definitions; this model also includes fields from later revisions.</seealso>
    /// <remarks>Unset fields are null. The model does not enforce ADIF numeric, date or enumeration constraints. The reader fills recognized properties, skips unknown fields and normalizes callsign/band casing. This model is not a guarantee of complete coverage of the latest ADIF schema.</remarks>
    public class AdifRow
    {
        /// <summary>Gets or sets the contacted station's complete mailing address: full name, street address, city, postal code, and country.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        public string ADDRESS { get; set; }

        /// <summary>Gets or sets the contacted station's complete mailing address: full name, street address, city, postal code, and country.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        public string ADDRESS_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's operator's age in years in the range 0 to 120 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string AGE { get; set; }

        /// <summary>Gets or sets the geomagnetic A index at the time of the QSO in the range 0 to 400 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string A_INDEX { get; set; }

        /// <summary>Gets or sets the logging station's antenna azimuth, in degrees with a value between 0 to 360 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 370 is exported as 10). True north is 0 degrees with values increasing in a clockwise direction.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string ANT_AZ { get; set; }

        /// <summary>Gets or sets the logging station's antenna elevation, in degrees with a value between -90 to 90 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 100 is exported as 80). The horizon is 0 degrees with values increasing as the angle moves in an upward direction.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string ANT_EL { get; set; }

        /// <summary>Gets or sets the signal path.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Ant Path</remarks>
        public string ANT_PATH { get; set; }

        /// <summary>Gets or sets the contacted station's ARRL section.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : ARRL Section</remarks>
        public string ARRL_SECT { get; set; }

        /// <summary>Gets or sets the list of awards submitted to a sponsor.  note that this field might not be used in a QSO record. It might be used to convey information about a user's "Award Account" between an award sponsor and the user. For example, AA6YQ might submit a request for awards by sending the following:  &lt;CALL:5&gt;AA6YQ &lt;AWARD_SUBMITTED:64&gt;ADIF_CENTURY_BASIC,ADIF_CENTURY_SILVER, ADIF_SPECTRUM_100-160m.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SponsoredAwardList. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Sponsored Award</remarks>
        public string AWARD_SUBMITTED { get; set; }

        /// <summary>Gets or sets the list of awards granted by a sponsor.  note that this field might not be used in a QSO record. It might be used to convey information about a user's "Award Account" between an award sponsor and the user. For example, in response to a request "send me a list of the awards granted to AA6YQ", this might be received:  &lt;CALL:5&gt;AA6YQ &lt;AWARD_GRANTED:64&gt;ADIF_CENTURY_BASIC,ADIF_CENTURY_SILVER, ADIF_SPECTRUM_100-160m.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SponsoredAwardList. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Sponsored Award</remarks>
        public string AWARD_GRANTED { get; set; }

        /// <summary>Gets or sets QSO Band.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Band</remarks>
        public string BAND { get; set; }

        /// <summary>Gets or sets in a split frequency QSO, the logging station's receiving band.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Band</remarks>
        public string BAND_RX { get; set; }

        /// <summary>Gets or sets the contacted station's Callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string CALL { get; set; }

        /// <summary>Gets or sets contest check (e.g. for ARRL Sweepstakes).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string CHECK { get; set; }

        /// <summary>Gets or sets contest class (e.g. for ARRL Field Day).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string CLASS { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the Club Log online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string CLUBLOG_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the Club Log online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        public string CLUBLOG_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the contacted station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Secondary Administrative Subdivision, function of DXCC field's value)</remarks>
        public string CNTY { get; set; }

        /// <summary>Gets or sets comment field for QSO.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string COMMENT { get; set; }

        /// <summary>Gets or sets comment field for QSO.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string COMMENT_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's Continent.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Continent</remarks>
        public string CONT { get; set; }

        /// <summary>Gets or sets the callsign of the individual operating the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string CONTACTED_OP { get; set; }

        /// <summary>Gets or sets QSO Contest Identifieruse enumeration values for interoperability.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Contest ID</remarks>
        public string CONTEST_ID { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string COUNTRY { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string COUNTRY_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's CQ Zone in the range 1 to 40 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string CQZ { get; set; }

        /// <summary>Gets or sets the list of credits sought for this QSO  Use of data type AwardList and enumeration Award are import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: CreditList  AwardList (import-only). The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Credit  Award (import-only)</remarks>
        public string CREDIT_SUBMITTED { get; set; }

        /// <summary>Gets or sets the list of credits granted to this QSO  Use of data type AwardList and enumeration Award are import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: CreditList  AwardList (import-only). The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Credit  Award (import-only)</remarks>
        public string CREDIT_GRANTED { get; set; }

        /// <summary>Gets or sets the contacted station's DARC DOK (District Location Code)  A DOK comprises letters and numbers, e.g. &lt;DARC_DOK:3&gt;A01.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (DOKs listed in DARC DOK List  Special DOKs listed in DARC Special DOK List)</remarks>
        public string DARC_DOK { get; set; }

        /// <summary>Gets or sets the distance between the logging station and the contacted station in kilometers via the specified signal path with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string DISTANCE { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC Entity Code  &lt;DXCC:1&gt;0 means that the contacted station is known not to be within a DXCC entity.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : DXCC Entity Code</remarks>
        public string DXCC { get; set; }

        /// <summary>Gets or sets the contacted station's email address.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string EMAIL { get; set; }

        /// <summary>Gets or sets the contacted station's owner's callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string EQ_CALL { get; set; }

        /// <summary>Gets or sets date QSL received from eQSL.cc(only valid if EQSL_QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string EQSL_QSLRDATE { get; set; }

        /// <summary>Gets or sets date QSL sent to eQSL.cc(only valid if EQSL_QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string EQSL_QSLSDATE { get; set; }

        /// <summary>Gets or sets eQSL.cc QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:42&gt;CQWAZ:eqsl,CQWAZ_BAND:eqsl,CQWAZ_MODE:eqsl  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        public string EQSL_QSL_RCVD { get; set; }

        /// <summary>Gets or sets eQSL.cc QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        public string EQSL_QSL_SENT { get; set; }

        /// <summary>Gets or sets the contacted station's FISTS CW Club member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string FISTS { get; set; }

        /// <summary>Gets or sets the contacted station's FISTS CW Club Century Certificate (CC) number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string FISTS_CC { get; set; }

        /// <summary>Gets or sets new EME "initial".</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        public string FORCE_INIT { get; set; }

        /// <summary>Gets or sets QSO frequency in Megahertz.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string FREQ { get; set; }

        /// <summary>Gets or sets in a split frequency QSO, the logging station's receiving frequency in Megahertz.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string FREQ_RX { get; set; }

        /// <summary>Gets or sets the contacted station's 2-character, 4-character, 6-character, or 8-character Maidenhead Grid Square.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquare. The model does not validate or convert this field.</value>
        public string GRIDSQUARE { get; set; }

        /// <summary>Gets or sets import-only: use OPERATOR instead.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string GUEST_OP { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the HRDLog.net online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string HRDLOG_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the HRDLog.net online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        public string HRDLOG_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the contacted station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 &lt;= XXX &lt;= 999 [use leading zeroes].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IOTARefNo. The model does not validate or convert this field.</value>
        public string IOTA { get; set; }

        /// <summary>Gets or sets the contacted station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string IOTA_ISLAND_ID { get; set; }

        /// <summary>Gets or sets the contacted station's ITU zone in the range 1 to 90 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string ITUZ { get; set; }

        /// <summary>Gets or sets the geomagnetic K index at the time of the QSO in the range 0 to 9 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string K_INDEX { get; set; }

        /// <summary>Gets or sets the contacted station's latitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        public string LAT { get; set; }

        /// <summary>Gets or sets the contacted station's longitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        public string LON { get; set; }

        /// <summary>Gets or sets date QSL received from ARRL Logbook of the World(only valid if LOTW_QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string LOTW_QSLRDATE { get; set; }

        /// <summary>Gets or sets date QSL sent to ARRL Logbook of the World(only valid if LOTW_QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string LOTW_QSLSDATE { get; set; }

        /// <summary>Gets or sets ARRL Logbook of the World QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:39&gt;DXCC:lotw,DXCC_BAND:lotw,DXCC_MODE:lotw  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        public string LOTW_QSL_RCVD { get; set; }

        /// <summary>Gets or sets ARRL Logbook of the World QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        public string LOTW_QSL_SENT { get; set; }

        /// <summary>Gets or sets maximum length of meteor scatter bursts heard by the logging station, in seconds with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string MAX_BURSTS { get; set; }

        /// <summary>Gets or sets QSO Mode.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Mode</remarks>
        public string MODE { get; set; }

        /// <summary>Gets or sets For Meteor Scatter QSOs, the name of the meteor shower in progress.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MS_SHOWER { get; set; }

        /// <summary>Gets or sets the logging station's antenna.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_ANTENNA { get; set; }

        /// <summary>Gets or sets the logging station's antenna.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_ANTENNA_INTL { get; set; }

        /// <summary>Gets or sets the logging station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_CITY { get; set; }

        /// <summary>Gets or sets the logging station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_CITY_INTL { get; set; }

        /// <summary>Gets or sets the logging station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Secondary Administrative Subdivision, function of MY_DXCC field's value)</remarks>
        public string MY_CNTY { get; set; }

        /// <summary>Gets or sets the logging station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Country</remarks>
        public string MY_COUNTRY { get; set; }

        /// <summary>Gets or sets the logging station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Country</remarks>
        public string MY_COUNTRY_INTL { get; set; }

        /// <summary>Gets or sets the logging station's CQ Zone in the range 1 to 40 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string MY_CQ_ZONE { get; set; }

        /// <summary>Gets or sets the logging station's DXCC Entity Code  &lt;MY_DXCC:1&gt;0 means that the logging station is known not to be within a DXCC entity.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : DXCC Entity Code</remarks>
        public string MY_DXCC { get; set; }

        /// <summary>Gets or sets the logging station's FISTS CW Club member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string MY_FISTS { get; set; }

        /// <summary>Gets or sets the logging station's 2-character, 4-character, 6-character, or 8-character Maidenhead Grid Square.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquare. The model does not validate or convert this field.</value>
        public string MY_GRIDSQUARE { get; set; }

        /// <summary>Gets or sets the logging station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 &lt;= XXX &lt;= 999 [use leading zeroes].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IOTARefNo. The model does not validate or convert this field.</value>
        public string MY_IOTA { get; set; }

        /// <summary>Gets or sets the logging station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string MY_IOTA_ISLAND_ID { get; set; }

        /// <summary>Gets or sets the logging station's ITU zone in the range 1 to 90 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string MY_ITU_ZONE { get; set; }

        /// <summary>Gets or sets the logging station's latitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        public string MY_LAT { get; set; }

        /// <summary>Gets or sets the logging station's longitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        public string MY_LON { get; set; }

        /// <summary>Gets or sets the logging operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_NAME { get; set; }

        /// <summary>Gets or sets the logging operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_NAME_INTL { get; set; }

        /// <summary>Gets or sets the logging station's postal code.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_POSTAL_CODE { get; set; }

        /// <summary>Gets or sets the logging station's postal code.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_POSTAL_CODE_INTL { get; set; }

        /// <summary>Gets or sets description of the logging station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_RIG { get; set; }

        /// <summary>Gets or sets description of the logging station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_RIG_INTL { get; set; }

        /// <summary>Gets or sets special interest activity or event.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_SIG { get; set; }

        /// <summary>Gets or sets special interest activity or event.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_SIG_INTL { get; set; }

        /// <summary>Gets or sets special interest activity or event information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_SIG_INFO { get; set; }

        /// <summary>Gets or sets special interest activity or event information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_SIG_INFO_INTL { get; set; }

        /// <summary>Gets or sets the logging station's International SOTA Reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SOTARef. The model does not validate or convert this field.</value>
        public string MY_SOTA_REF { get; set; }

        /// <summary>Gets or sets the code for the logging station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Primary Administrative Subdivision, function of MY_DXCC field's value)</remarks>
        public string MY_STATE { get; set; }

        /// <summary>Gets or sets the logging station's street.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string MY_STREET { get; set; }

        /// <summary>Gets or sets the logging station's street.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string MY_STREET_INTL { get; set; }

        /// <summary>Gets or sets two US counties in the case where the logging station is located on a border between two counties, representing counties that the contacted station may claim for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondarySubdivisionList. The model does not validate or convert this field.</value>
        public string MY_USACA_COUNTIES { get; set; }

        /// <summary>Gets or sets two or four adjacent Maidenhead grid locators, each four characters long, representing the logging station's grid squares that the contacted station may claim for the ARRL VUCC award program. E.g.  EN98,FM08,EM97,FM07.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareList. The model does not validate or convert this field.</value>
        public string MY_VUCC_GRIDS { get; set; }

        /// <summary>Gets or sets the contacted station's operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string NAME { get; set; }

        /// <summary>Gets or sets the contacted station's operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string NAME_INTL { get; set; }

        /// <summary>Gets or sets QSO notes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        public string NOTES { get; set; }

        /// <summary>Gets or sets QSO notes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        public string NOTES_INTL { get; set; }

        /// <summary>Gets or sets the number of meteor scatter bursts heard by the logging station with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string NR_BURSTS { get; set; }

        /// <summary>Gets or sets the number of meteor scatter pings heard by the logging station with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string NR_PINGS { get; set; }

        /// <summary>Gets or sets the logging operator's callsignif STATION_CALLSIGN is absent, OPERATOR shall be treated as both the logging station's callsign and the logging operator's callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string OPERATOR { get; set; }

        /// <summary>Gets or sets the callsign of the owner of the station used to log the contact (the callsign of the OPERATOR's host)if OWNER_CALLSIGN is absent, STATION_CALLSIGN shall be treated as both the logging station's callsign and the callsign of the owner of the station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string OWNER_CALLSIGN { get; set; }

        /// <summary>Gets or sets the contacted station's WPX prefix.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string PFX { get; set; }

        /// <summary>Gets or sets contest precedence (e.g. for ARRL Sweepstakes).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string PRECEDENCE { get; set; }

        /// <summary>Gets or sets QSO propagation mode.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Propagation Mode</remarks>
        public string PROP_MODE { get; set; }

        /// <summary>Gets or sets public encryption key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string PUBLIC_KEY { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the QRZ.COM online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string QRZCOM_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the QRZ.COM online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        public string QRZCOM_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets QSL card message.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        public string QSLMSG { get; set; }

        /// <summary>Gets or sets QSL card message.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        public string QSLMSG_INTL { get; set; }

        /// <summary>Gets or sets QSL received date(only valid if QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string QSLRDATE { get; set; }

        /// <summary>Gets or sets QSL sent date(only valid if QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string QSLSDATE { get; set; }

        /// <summary>Gets or sets QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:39&gt;DXCC:card,DXCC_BAND:card,DXCC_MODE:card  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        public string QSL_RCVD { get; set; }

        /// <summary>Gets or sets means by which the QSL was received by the logging station  use of M (manager) is import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Via</remarks>
        public string QSL_RCVD_VIA { get; set; }

        /// <summary>Gets or sets QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        public string QSL_SENT { get; set; }

        /// <summary>Gets or sets means by which the QSL was sent by the logging station  use of M (manager) is import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Via</remarks>
        public string QSL_SENT_VIA { get; set; }

        /// <summary>Gets or sets the contacted station's QSL route.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string QSL_VIA { get; set; }

        /// <summary>Gets or sets indicates whether the QSO was complete from the perspective of the logging stationY - yes N - no NIL - not heard ? - uncertain.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Complete</remarks>
        public string QSO_COMPLETE { get; set; }

        /// <summary>Gets or sets date on which the QSO started.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string QSO_DATE { get; set; }

        /// <summary>Gets or sets date on which the QSO ended.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        public string QSO_DATE_OFF { get; set; }

        /// <summary>Gets or sets indicates whether the QSO was random or scheduled.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        public string QSO_RANDOM { get; set; }

        /// <summary>Gets or sets the contacted station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string QTH { get; set; }

        /// <summary>Gets or sets the contacted station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string QTH_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's WAE or CQ entity contained within a DXCC entity.the value None indicates that the WAE or CQ entity is the DXCC entity in the DXCC field. nothing can be inferred from the absence of the REGION field.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Region</remarks>
        public string REGION { get; set; }

        /// <summary>Gets or sets description of the contacted station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        public string RIG { get; set; }

        /// <summary>Gets or sets description of the contacted station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        public string RIG_INTL { get; set; }

        /// <summary>Gets or sets signal report from the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string RST_RCVD { get; set; }

        /// <summary>Gets or sets signal report sent to the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string RST_SENT { get; set; }

        /// <summary>Gets or sets the contacted station's transmitter power in Watts with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string RX_PWR { get; set; }

        /// <summary>Gets or sets satellite mode.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SAT_MODE { get; set; }

        /// <summary>Gets or sets name of satellite.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SAT_NAME { get; set; }

        /// <summary>Gets or sets the solar flux at the time of the QSO in the range 0 to 300 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string SFI { get; set; }

        /// <summary>Gets or sets the name of the contacted station's special activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SIG { get; set; }

        /// <summary>Gets or sets the name of the contacted station's special activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string SIG_INTL { get; set; }

        /// <summary>Gets or sets information associated with the contacted station's activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SIG_INFO { get; set; }

        /// <summary>Gets or sets information associated with the contacted station's activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        public string SIG_INFO_INTL { get; set; }

        /// <summary>Gets or sets Y' indicates that the contacted station's operator is now a Silent Key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        public string SILENT_KEY { get; set; }

        /// <summary>Gets or sets the contacted station's Straight Key Century Club (SKCC) member information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SKCC { get; set; }

        /// <summary>Gets or sets the contacted station's International SOTA Reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SOTARef. The model does not validate or convert this field.</value>
        public string SOTA_REF { get; set; }

        /// <summary>Gets or sets contest QSO received serial number with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string SRX { get; set; }

        /// <summary>Gets or sets contest QSO received informationuse Cabrillo format to convey contest information for which ADIF fields are not specified in the event of a conflict between information in a dedicated contest field and this field, information in the dedicated contest field shall prevail.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string SRX_STRING { get; set; }

        /// <summary>Gets or sets the code for the contacted station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Primary Administrative Subdivision, function of DXCC field's value)</remarks>
        public string STATE { get; set; }

        /// <summary>Gets or sets the logging station's callsign (the callsign used over the air)if STATION_CALLSIGN is absent, OPERATOR shall be treated as both the logging station's callsign and the logging operator's callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string STATION_CALLSIGN { get; set; }

        /// <summary>Gets or sets contest QSO transmitted serial number with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        public string STX { get; set; }

        /// <summary>Gets or sets contest QSO transmitted informationuse Cabrillo format to convey contest information for which ADIF fields are not specified in the event of a conflict between information in a dedicated contest field and this field, information in the dedicated contest field shall prevail.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string STX_STRING { get; set; }

        /// <summary>Gets or sets QSO Submode  use enumeration values for interoperability.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Submode (function of MODE field's value)</remarks>
        public string SUBMODE { get; set; }

        /// <summary>Gets or sets indicates that the QSO information pertains to an SWL report.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        public string SWL { get; set; }

        /// <summary>Gets or sets Ten-Ten number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string TEN_TEN { get; set; }

        /// <summary>Gets or sets HHMM or HHMMSS in UTCin the absence of &lt;QSO_DATE_OFF&gt;, the QSO duration is less than 24 hours.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Time. The model does not validate or convert this field.</value>
        public string TIME_OFF { get; set; }

        /// <summary>Gets or sets HHMM or HHMMSS in UTC.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Time. The model does not validate or convert this field.</value>
        public string TIME_ON { get; set; }

        /// <summary>Gets or sets the logging station's power in Watts with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        public string TX_PWR { get; set; }

        /// <summary>Gets or sets the contacted station's UKSMG member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        public string UKSMG { get; set; }

        /// <summary>Gets or sets two US counties in the case where the contacted station is located on a border between two counties, representing counties credited to the QSO for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondarySubdivisionList. The model does not validate or convert this field.</value>
        public string USACA_COUNTIES { get; set; }

        /// <summary>Gets or sets import-only: use STATE instead.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string VE_PROV { get; set; }

        /// <summary>Gets or sets two or four adjacent Maidenhead grid locators, each four characters long, representing the contacted station's grid squares credited to the QSO for the ARRL VUCC award program. E.g.  EN98,FM08,EM97,FM07.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareList. The model does not validate or convert this field.</value>
        public string VUCC_GRIDS { get; set; }

        /// <summary>Gets or sets the contacted station's URL.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        public string WEB { get; set; }
    }
}
