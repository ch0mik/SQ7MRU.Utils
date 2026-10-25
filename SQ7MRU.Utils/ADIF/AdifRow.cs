#nullable disable
namespace SQ7MRU.Utils
{
    /// <summary>
    /// Represents all 181 QSO fields in the reviewed ADIF 3.1.8 specification as optional raw strings.
    /// </summary>
    /// <seealso href="https://www.adif.org/318/ADIF_318.htm">Reviewed QSO field definitions, types and import/export restrictions.</seealso>
    /// <remarks>Unset fields are null. The model does not enforce ADIF numeric, date or enumeration constraints. The reader fills recognized properties, skips unknown fields and normalizes callsign/band casing. Raw property assignment is not validation. Standard document import/export validates the supported profile; GetAdifRows retains provider compatibility. International fields are ADX-only and rejected by standard ADI export.</remarks>
    public class AdifRow
    {
        /// <summary>Gets or sets the contacted station's complete mailing address: full name, street address, city, postal code, and country.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ADDRESS">Normative field definition and constraints.</seealso>
        public string ADDRESS { get; set; }

        /// <summary>Gets or sets the contacted station's complete mailing address: full name, street address, city, postal code, and country.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ADDRESS_INTL">Normative field definition and constraints.</seealso>
        public string ADDRESS_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's operator's age in years in the range 0 to 120 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_AGE">Normative field definition and constraints.</seealso>
        public string AGE { get; set; }

        /// <summary>Gets or sets the geomagnetic A index at the time of the QSO in the range 0 to 400 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_A_INDEX">Normative field definition and constraints.</seealso>
        public string A_INDEX { get; set; }

        /// <summary>Gets or sets the logging station's antenna azimuth, in degrees with a value between 0 to 360 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 370 is exported as 10). True north is 0 degrees with values increasing in a clockwise direction.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ANT_AZ">Normative field definition and constraints.</seealso>
        public string ANT_AZ { get; set; }

        /// <summary>Gets or sets the logging station's antenna elevation, in degrees with a value between -90 to 90 (inclusive). Values outside this range are import-only and must be normalized for export (e.g. 100 is exported as 80). The horizon is 0 degrees with values increasing as the angle moves in an upward direction.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ANT_EL">Normative field definition and constraints.</seealso>
        public string ANT_EL { get; set; }

        /// <summary>Gets or sets the signal path.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Ant Path</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ANT_PATH">Normative field definition and constraints.</seealso>
        public string ANT_PATH { get; set; }

        /// <summary>Gets or sets the contacted station's ARRL section.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : ARRL Section</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ARRL_SECT">Normative field definition and constraints.</seealso>
        public string ARRL_SECT { get; set; }

        /// <summary>Gets or sets awards submitted to a sponsor, which may describe an award account rather than an individual QSO.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SponsoredAwardList. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Sponsored Award</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_AWARD_SUBMITTED">Normative field definition and constraints.</seealso>
        public string AWARD_SUBMITTED { get; set; }

        /// <summary>Gets or sets sponsor-granted awards, which may describe an award account rather than an individual QSO.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SponsoredAwardList. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Sponsored Award</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_AWARD_GRANTED">Normative field definition and constraints.</seealso>
        public string AWARD_GRANTED { get; set; }

        /// <summary>Gets or sets QSO Band.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Band</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_BAND">Normative field definition and constraints.</seealso>
        public string BAND { get; set; }

        /// <summary>Gets or sets in a split frequency QSO, the logging station's receiving band.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Band</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_BAND_RX">Normative field definition and constraints.</seealso>
        public string BAND_RX { get; set; }

        /// <summary>Gets or sets the contacted station's Callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CALL">Normative field definition and constraints.</seealso>
        public string CALL { get; set; }

        /// <summary>Gets or sets contest check (e.g. for ARRL Sweepstakes).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CHECK">Normative field definition and constraints.</seealso>
        public string CHECK { get; set; }

        /// <summary>Gets or sets contest class (e.g. for ARRL Field Day).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CLASS">Normative field definition and constraints.</seealso>
        public string CLASS { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the Club Log online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CLUBLOG_QSO_UPLOAD_DATE">Normative field definition and constraints.</seealso>
        public string CLUBLOG_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the Club Log online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CLUBLOG_QSO_UPLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string CLUBLOG_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the contacted station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Secondary Administrative Subdivision, function of DXCC field's value)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CNTY">Normative field definition and constraints.</seealso>
        public string CNTY { get; set; }

        /// <summary>Gets or sets QSO information intended for the contacted operator; use QSLMSG for a message to print on a QSL.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_COMMENT">Normative field definition and constraints.</seealso>
        public string COMMENT { get; set; }

        /// <summary>Gets or sets international QSO information intended for the contacted operator; use QSLMSG_INTL for a QSL message.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_COMMENT_INTL">Normative field definition and constraints.</seealso>
        public string COMMENT_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's Continent.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Continent</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CONT">Normative field definition and constraints.</seealso>
        public string CONT { get; set; }

        /// <summary>Gets or sets the callsign of the individual operating the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CONTACTED_OP">Normative field definition and constraints.</seealso>
        public string CONTACTED_OP { get; set; }

        /// <summary>Gets or sets the contest identifier from the reviewed enumeration.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Contest ID</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CONTEST_ID">Normative field definition and constraints.</seealso>
        public string CONTEST_ID { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_COUNTRY">Normative field definition and constraints.</seealso>
        public string COUNTRY { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_COUNTRY_INTL">Normative field definition and constraints.</seealso>
        public string COUNTRY_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's CQ Zone in the range 1 to 40 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CQZ">Normative field definition and constraints.</seealso>
        public string CQZ { get; set; }

        /// <summary>Gets or sets the list of credits sought for this QSO  Use of data type AwardList and enumeration Award are import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: CreditList  AwardList (import-only). The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Credit  Award (import-only)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CREDIT_SUBMITTED">Normative field definition and constraints.</seealso>
        public string CREDIT_SUBMITTED { get; set; }

        /// <summary>Gets or sets the list of credits granted to this QSO  Use of data type AwardList and enumeration Award are import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: CreditList  AwardList (import-only). The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Credit  Award (import-only)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CREDIT_GRANTED">Normative field definition and constraints.</seealso>
        public string CREDIT_GRANTED { get; set; }

        /// <summary>Gets or sets the contacted station's DARC DOK (District Location Code)  A DOK comprises letters and numbers, e.g. &lt;DARC_DOK:3&gt;A01.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (DOKs listed in DARC DOK List  Special DOKs listed in DARC Special DOK List)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DARC_DOK">Normative field definition and constraints.</seealso>
        public string DARC_DOK { get; set; }

        /// <summary>Gets or sets the distance between the logging station and the contacted station in kilometers via the specified signal path with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DISTANCE">Normative field definition and constraints.</seealso>
        public string DISTANCE { get; set; }

        /// <summary>Gets or sets the contacted station's DXCC Entity Code  &lt;DXCC:1&gt;0 means that the contacted station is known not to be within a DXCC entity.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : DXCC Entity Code</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DXCC">Normative field definition and constraints.</seealso>
        public string DXCC { get; set; }

        /// <summary>Gets or sets the contacted station's email address.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EMAIL">Normative field definition and constraints.</seealso>
        public string EMAIL { get; set; }

        /// <summary>Gets or sets the contacted station's owner's callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQ_CALL">Normative field definition and constraints.</seealso>
        public string EQ_CALL { get; set; }

        /// <summary>Gets or sets date QSL received from eQSL.cc(only valid if EQSL_QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQSL_QSLRDATE">Normative field definition and constraints.</seealso>
        public string EQSL_QSLRDATE { get; set; }

        /// <summary>Gets or sets date QSL sent to eQSL.cc(only valid if EQSL_QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQSL_QSLSDATE">Normative field definition and constraints.</seealso>
        public string EQSL_QSLSDATE { get; set; }

        /// <summary>Gets or sets eQSL.cc QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:42&gt;CQWAZ:eqsl,CQWAZ_BAND:eqsl,CQWAZ_MODE:eqsl  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQSL_QSL_RCVD">Normative field definition and constraints.</seealso>
        public string EQSL_QSL_RCVD { get; set; }

        /// <summary>Gets or sets eQSL.cc QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQSL_QSL_SENT">Normative field definition and constraints.</seealso>
        public string EQSL_QSL_SENT { get; set; }

        /// <summary>Gets or sets the contacted station's FISTS CW Club member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_FISTS">Normative field definition and constraints.</seealso>
        public string FISTS { get; set; }

        /// <summary>Gets or sets the contacted station's FISTS CW Club Century Certificate (CC) number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_FISTS_CC">Normative field definition and constraints.</seealso>
        public string FISTS_CC { get; set; }

        /// <summary>Gets or sets new EME "initial".</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_FORCE_INIT">Normative field definition and constraints.</seealso>
        public string FORCE_INIT { get; set; }

        /// <summary>Gets or sets QSO frequency in Megahertz.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_FREQ">Normative field definition and constraints.</seealso>
        public string FREQ { get; set; }

        /// <summary>Gets or sets in a split frequency QSO, the logging station's receiving frequency in Megahertz.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_FREQ_RX">Normative field definition and constraints.</seealso>
        public string FREQ_RX { get; set; }

        /// <summary>Gets or sets the contacted station 2/4/6/8-character Maidenhead locator; longer locators use GRIDSQUARE_EXT for the remaining characters.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquare. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_GRIDSQUARE">Normative field definition and constraints.</seealso>
        public string GRIDSQUARE { get; set; }

        /// <summary>Gets or sets import-only: use OPERATOR instead.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Import-only compatibility field. Standard export requires an explicit replacement; it does not silently discard this property.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_GUEST_OP">Normative field definition and constraints.</seealso>
        public string GUEST_OP { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the HRDLog.net online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HRDLOG_QSO_UPLOAD_DATE">Normative field definition and constraints.</seealso>
        public string HRDLOG_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the HRDLog.net online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HRDLOG_QSO_UPLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string HRDLOG_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the contacted station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 &lt;= XXX &lt;= 999 [use leading zeroes].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IOTARefNo. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_IOTA">Normative field definition and constraints.</seealso>
        public string IOTA { get; set; }

        /// <summary>Gets or sets the contacted station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_IOTA_ISLAND_ID">Normative field definition and constraints.</seealso>
        public string IOTA_ISLAND_ID { get; set; }

        /// <summary>Gets or sets the contacted station's ITU zone in the range 1 to 90 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ITUZ">Normative field definition and constraints.</seealso>
        public string ITUZ { get; set; }

        /// <summary>Gets or sets the geomagnetic K index at the time of the QSO in the range 0 to 9 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_K_INDEX">Normative field definition and constraints.</seealso>
        public string K_INDEX { get; set; }

        /// <summary>Gets or sets the contacted station's latitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LAT">Normative field definition and constraints.</seealso>
        public string LAT { get; set; }

        /// <summary>Gets or sets the contacted station's longitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LON">Normative field definition and constraints.</seealso>
        public string LON { get; set; }

        /// <summary>Gets or sets date QSL received from ARRL Logbook of the World(only valid if LOTW_QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LOTW_QSLRDATE">Normative field definition and constraints.</seealso>
        public string LOTW_QSLRDATE { get; set; }

        /// <summary>Gets or sets date QSL sent to ARRL Logbook of the World(only valid if LOTW_QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LOTW_QSLSDATE">Normative field definition and constraints.</seealso>
        public string LOTW_QSLSDATE { get; set; }

        /// <summary>Gets or sets ARRL Logbook of the World QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:39&gt;DXCC:lotw,DXCC_BAND:lotw,DXCC_MODE:lotw  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LOTW_QSL_RCVD">Normative field definition and constraints.</seealso>
        public string LOTW_QSL_RCVD { get; set; }

        /// <summary>Gets or sets ARRL Logbook of the World QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_LOTW_QSL_SENT">Normative field definition and constraints.</seealso>
        public string LOTW_QSL_SENT { get; set; }

        /// <summary>Gets or sets maximum length of meteor scatter bursts heard by the logging station, in seconds with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MAX_BURSTS">Normative field definition and constraints.</seealso>
        public string MAX_BURSTS { get; set; }

        /// <summary>Gets or sets QSO Mode.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Mode</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MODE">Normative field definition and constraints.</seealso>
        public string MODE { get; set; }

        /// <summary>Gets or sets For Meteor Scatter QSOs, the name of the meteor shower in progress.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MS_SHOWER">Normative field definition and constraints.</seealso>
        public string MS_SHOWER { get; set; }

        /// <summary>Gets or sets the logging station's antenna.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_ANTENNA">Normative field definition and constraints.</seealso>
        public string MY_ANTENNA { get; set; }

        /// <summary>Gets or sets the logging station's antenna.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_ANTENNA_INTL">Normative field definition and constraints.</seealso>
        public string MY_ANTENNA_INTL { get; set; }

        /// <summary>Gets or sets the logging station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_CITY">Normative field definition and constraints.</seealso>
        public string MY_CITY { get; set; }

        /// <summary>Gets or sets the logging station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_CITY_INTL">Normative field definition and constraints.</seealso>
        public string MY_CITY_INTL { get; set; }

        /// <summary>Gets or sets the logging station's Secondary Administrative Subdivision (e.g. US county, JA Gun), in the specified format.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Secondary Administrative Subdivision, function of MY_DXCC field's value)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_CNTY">Normative field definition and constraints.</seealso>
        public string MY_CNTY { get; set; }

        /// <summary>Gets or sets the logging station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Country</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_COUNTRY">Normative field definition and constraints.</seealso>
        public string MY_COUNTRY { get; set; }

        /// <summary>Gets or sets the logging station's DXCC entity name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Country</remarks>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_COUNTRY_INTL">Normative field definition and constraints.</seealso>
        public string MY_COUNTRY_INTL { get; set; }

        /// <summary>Gets or sets the logging station's CQ Zone in the range 1 to 40 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_CQ_ZONE">Normative field definition and constraints.</seealso>
        public string MY_CQ_ZONE { get; set; }

        /// <summary>Gets or sets the logging station's DXCC Entity Code  &lt;MY_DXCC:1&gt;0 means that the logging station is known not to be within a DXCC entity.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : DXCC Entity Code</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_DXCC">Normative field definition and constraints.</seealso>
        public string MY_DXCC { get; set; }

        /// <summary>Gets or sets the logging station's FISTS CW Club member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_FISTS">Normative field definition and constraints.</seealso>
        public string MY_FISTS { get; set; }

        /// <summary>Gets or sets the logging station 2/4/6/8-character Maidenhead locator; longer locators use MY_GRIDSQUARE_EXT for the remaining characters.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquare. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_GRIDSQUARE">Normative field definition and constraints.</seealso>
        public string MY_GRIDSQUARE { get; set; }

        /// <summary>Gets or sets the logging station's IOTA designator, in format CC-XXX, whereCC is a member of the Continent enumeration XXX is the island group designator, where 1 &lt;= XXX &lt;= 999 [use leading zeroes].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IOTARefNo. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_IOTA">Normative field definition and constraints.</seealso>
        public string MY_IOTA { get; set; }

        /// <summary>Gets or sets the logging station's IOTA Island Identifier, an 8-digit integer in the range 1 to 99999999 [leading zeroes optional].</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_IOTA_ISLAND_ID">Normative field definition and constraints.</seealso>
        public string MY_IOTA_ISLAND_ID { get; set; }

        /// <summary>Gets or sets the logging station's ITU zone in the range 1 to 90 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_ITU_ZONE">Normative field definition and constraints.</seealso>
        public string MY_ITU_ZONE { get; set; }

        /// <summary>Gets or sets the logging station's latitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_LAT">Normative field definition and constraints.</seealso>
        public string MY_LAT { get; set; }

        /// <summary>Gets or sets the logging station's longitude.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Location. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_LON">Normative field definition and constraints.</seealso>
        public string MY_LON { get; set; }

        /// <summary>Gets or sets the logging operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_NAME">Normative field definition and constraints.</seealso>
        public string MY_NAME { get; set; }

        /// <summary>Gets or sets the logging operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_NAME_INTL">Normative field definition and constraints.</seealso>
        public string MY_NAME_INTL { get; set; }

        /// <summary>Gets or sets the logging station's postal code.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_POSTAL_CODE">Normative field definition and constraints.</seealso>
        public string MY_POSTAL_CODE { get; set; }

        /// <summary>Gets or sets the logging station's postal code.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_POSTAL_CODE_INTL">Normative field definition and constraints.</seealso>
        public string MY_POSTAL_CODE_INTL { get; set; }

        /// <summary>Gets or sets description of the logging station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_RIG">Normative field definition and constraints.</seealso>
        public string MY_RIG { get; set; }

        /// <summary>Gets or sets description of the logging station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_RIG_INTL">Normative field definition and constraints.</seealso>
        public string MY_RIG_INTL { get; set; }

        /// <summary>Gets or sets special interest activity or event.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_SIG">Normative field definition and constraints.</seealso>
        public string MY_SIG { get; set; }

        /// <summary>Gets or sets special interest activity or event.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_SIG_INTL">Normative field definition and constraints.</seealso>
        public string MY_SIG_INTL { get; set; }

        /// <summary>Gets or sets special interest activity or event information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_SIG_INFO">Normative field definition and constraints.</seealso>
        public string MY_SIG_INFO { get; set; }

        /// <summary>Gets or sets special interest activity or event information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_SIG_INFO_INTL">Normative field definition and constraints.</seealso>
        public string MY_SIG_INFO_INTL { get; set; }

        /// <summary>Gets or sets the logging station's International SOTA Reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SOTARef. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_SOTA_REF">Normative field definition and constraints.</seealso>
        public string MY_SOTA_REF { get; set; }

        /// <summary>Gets or sets the code for the logging station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Primary Administrative Subdivision, function of MY_DXCC field's value)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_STATE">Normative field definition and constraints.</seealso>
        public string MY_STATE { get; set; }

        /// <summary>Gets or sets the logging station's street.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_STREET">Normative field definition and constraints.</seealso>
        public string MY_STREET { get; set; }

        /// <summary>Gets or sets the logging station's street.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_STREET_INTL">Normative field definition and constraints.</seealso>
        public string MY_STREET_INTL { get; set; }

        /// <summary>Gets or sets two US counties in the case where the logging station is located on a border between two counties, representing counties that the contacted station may claim for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondarySubdivisionList. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_USACA_COUNTIES">Normative field definition and constraints.</seealso>
        public string MY_USACA_COUNTIES { get; set; }

        /// <summary>Gets or sets two or four adjacent 4-character or 6-character locators for the logging station that the contacted station may claim for VUCC.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareList. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_VUCC_GRIDS">Normative field definition and constraints.</seealso>
        public string MY_VUCC_GRIDS { get; set; }

        /// <summary>Gets or sets the contacted station's operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NAME">Normative field definition and constraints.</seealso>
        public string NAME { get; set; }

        /// <summary>Gets or sets the contacted station's operator's name.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NAME_INTL">Normative field definition and constraints.</seealso>
        public string NAME_INTL { get; set; }

        /// <summary>Gets or sets QSO notes intended for the logging operator.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NOTES">Normative field definition and constraints.</seealso>
        public string NOTES { get; set; }

        /// <summary>Gets or sets international QSO notes intended for the logging operator.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NOTES_INTL">Normative field definition and constraints.</seealso>
        public string NOTES_INTL { get; set; }

        /// <summary>Gets or sets the number of meteor scatter bursts heard by the logging station with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NR_BURSTS">Normative field definition and constraints.</seealso>
        public string NR_BURSTS { get; set; }

        /// <summary>Gets or sets the number of meteor scatter pings heard by the logging station with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_NR_PINGS">Normative field definition and constraints.</seealso>
        public string NR_PINGS { get; set; }

        /// <summary>Gets or sets the logging operator callsign; when STATION_CALLSIGN is absent it also identifies the on-air logging callsign.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_OPERATOR">Normative field definition and constraints.</seealso>
        public string OPERATOR { get; set; }

        /// <summary>Gets or sets the callsign of the owner of the station used to log the contact (the callsign of the OPERATOR's host)if OWNER_CALLSIGN is absent, STATION_CALLSIGN shall be treated as both the logging station's callsign and the callsign of the owner of the station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_OWNER_CALLSIGN">Normative field definition and constraints.</seealso>
        public string OWNER_CALLSIGN { get; set; }

        /// <summary>Gets or sets the contacted station's WPX prefix.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_PFX">Normative field definition and constraints.</seealso>
        public string PFX { get; set; }

        /// <summary>Gets or sets contest precedence (e.g. for ARRL Sweepstakes).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_PRECEDENCE">Normative field definition and constraints.</seealso>
        public string PRECEDENCE { get; set; }

        /// <summary>Gets or sets QSO propagation mode.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Propagation Mode</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_PROP_MODE">Normative field definition and constraints.</seealso>
        public string PROP_MODE { get; set; }

        /// <summary>Gets or sets public encryption key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_PUBLIC_KEY">Normative field definition and constraints.</seealso>
        public string PUBLIC_KEY { get; set; }

        /// <summary>Gets or sets the date the QSO was last uploaded to the QRZ.COM online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QRZCOM_QSO_UPLOAD_DATE">Normative field definition and constraints.</seealso>
        public string QRZCOM_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the upload status of the QSO on the QRZ.COM online service.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Upload Status</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QRZCOM_QSO_UPLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string QRZCOM_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets QSL card message.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSLMSG">Normative field definition and constraints.</seealso>
        public string QSLMSG { get; set; }

        /// <summary>Gets or sets QSL card message.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSLMSG_INTL">Normative field definition and constraints.</seealso>
        public string QSLMSG_INTL { get; set; }

        /// <summary>Gets or sets QSL received date(only valid if QSL_RCVD is Y, I, or V) (V import-only).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSLRDATE">Normative field definition and constraints.</seealso>
        public string QSLRDATE { get; set; }

        /// <summary>Gets or sets QSL sent date(only valid if QSL_SENT is Y, Q, or I).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSLSDATE">Normative field definition and constraints.</seealso>
        public string QSLSDATE { get; set; }

        /// <summary>Gets or sets QSL received status  instead of V (import-only) use &lt;CREDIT_GRANTED:39&gt;DXCC:card,DXCC_BAND:card,DXCC_MODE:card  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Rcvd</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSL_RCVD">Normative field definition and constraints.</seealso>
        public string QSL_RCVD { get; set; }

        /// <summary>Gets or sets the received medium when QSL_RCVD is Y or import-only V; otherwise the requested or intended medium. Manager value M is import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Via</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSL_RCVD_VIA">Normative field definition and constraints.</seealso>
        public string QSL_RCVD_VIA { get; set; }

        /// <summary>Gets or sets QSL sent status  Default Value: N.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Sent</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSL_SENT">Normative field definition and constraints.</seealso>
        public string QSL_SENT { get; set; }

        /// <summary>Gets or sets the sent medium when QSL_SENT is Y; otherwise the intended medium. Manager value M is import-only.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSL Via</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSL_SENT_VIA">Normative field definition and constraints.</seealso>
        public string QSL_SENT_VIA { get; set; }

        /// <summary>Gets or sets the contacted station's QSL route.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSL_VIA">Normative field definition and constraints.</seealso>
        public string QSL_VIA { get; set; }

        /// <summary>Gets or sets QSO completion status: Y, N, NIL or question mark; no status is inferred from a null property.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : QSO Complete</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSO_COMPLETE">Normative field definition and constraints.</seealso>
        public string QSO_COMPLETE { get; set; }

        /// <summary>Gets or sets date on which the QSO started.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSO_DATE">Normative field definition and constraints.</seealso>
        public string QSO_DATE { get; set; }

        /// <summary>Gets or sets date on which the QSO ended.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSO_DATE_OFF">Normative field definition and constraints.</seealso>
        public string QSO_DATE_OFF { get; set; }

        /// <summary>Gets or sets indicates whether the QSO was random or scheduled.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSO_RANDOM">Normative field definition and constraints.</seealso>
        public string QSO_RANDOM { get; set; }

        /// <summary>Gets or sets the contacted station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QTH">Normative field definition and constraints.</seealso>
        public string QTH { get; set; }

        /// <summary>Gets or sets the contacted station's city.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QTH_INTL">Normative field definition and constraints.</seealso>
        public string QTH_INTL { get; set; }

        /// <summary>Gets or sets the contacted station's WAE or CQ entity contained within a DXCC entity.the value None indicates that the WAE or CQ entity is the DXCC entity in the DXCC field. nothing can be inferred from the absence of the REGION field.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Region</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_REGION">Normative field definition and constraints.</seealso>
        public string REGION { get; set; }

        /// <summary>Gets or sets description of the contacted station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_RIG">Normative field definition and constraints.</seealso>
        public string RIG { get; set; }

        /// <summary>Gets or sets description of the contacted station's equipment.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlMultilineString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_RIG_INTL">Normative field definition and constraints.</seealso>
        public string RIG_INTL { get; set; }

        /// <summary>Gets or sets signal report from the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_RST_RCVD">Normative field definition and constraints.</seealso>
        public string RST_RCVD { get; set; }

        /// <summary>Gets or sets signal report sent to the contacted station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_RST_SENT">Normative field definition and constraints.</seealso>
        public string RST_SENT { get; set; }

        /// <summary>Gets or sets the contacted station transmitter power in watts, including zero.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_RX_PWR">Normative field definition and constraints.</seealso>
        public string RX_PWR { get; set; }

        /// <summary>Gets or sets the satellite uplink/downlink band code, distinct from the QSO modulation MODE.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SAT_MODE">Normative field definition and constraints.</seealso>
        public string SAT_MODE { get; set; }

        /// <summary>Gets or sets name of satellite.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SAT_NAME">Normative field definition and constraints.</seealso>
        public string SAT_NAME { get; set; }

        /// <summary>Gets or sets the solar flux at the time of the QSO in the range 0 to 300 (inclusive).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SFI">Normative field definition and constraints.</seealso>
        public string SFI { get; set; }

        /// <summary>Gets or sets the name of the contacted station's special activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SIG">Normative field definition and constraints.</seealso>
        public string SIG { get; set; }

        /// <summary>Gets or sets the name of the contacted station's special activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SIG_INTL">Normative field definition and constraints.</seealso>
        public string SIG_INTL { get; set; }

        /// <summary>Gets or sets information associated with the contacted station's activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SIG_INFO">Normative field definition and constraints.</seealso>
        public string SIG_INFO { get; set; }

        /// <summary>Gets or sets information associated with the contacted station's activity or interest group.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: IntlString. The model does not validate or convert this field.</value>
        /// <remarks>ADX-only international data; standard ADI import/export rejects this field. The provider compatibility profile can preserve it without a conformance claim.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SIG_INFO_INTL">Normative field definition and constraints.</seealso>
        public string SIG_INFO_INTL { get; set; }

        /// <summary>Gets or sets Y' indicates that the contacted station's operator is now a Silent Key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SILENT_KEY">Normative field definition and constraints.</seealso>
        public string SILENT_KEY { get; set; }

        /// <summary>Gets or sets the contacted station's Straight Key Century Club (SKCC) member information.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SKCC">Normative field definition and constraints.</seealso>
        public string SKCC { get; set; }

        /// <summary>Gets or sets the contacted station's International SOTA Reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SOTARef. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SOTA_REF">Normative field definition and constraints.</seealso>
        public string SOTA_REF { get; set; }

        /// <summary>Gets or sets contest QSO received serial number with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SRX">Normative field definition and constraints.</seealso>
        public string SRX { get; set; }

        /// <summary>Gets or sets received contest exchange text; dedicated contest fields take precedence over conflicting exchange text.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SRX_STRING">Normative field definition and constraints.</seealso>
        public string SRX_STRING { get; set; }

        /// <summary>Gets or sets the code for the contacted station's Primary Administrative Subdivision (e.g. US State, JA Island, VE Province).</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : (Primary Administrative Subdivision, function of DXCC field's value)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_STATE">Normative field definition and constraints.</seealso>
        public string STATE { get; set; }

        /// <summary>Gets or sets the on-air logging callsign, distinct from OPERATOR when another operator uses the station.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_STATION_CALLSIGN">Normative field definition and constraints.</seealso>
        public string STATION_CALLSIGN { get; set; }

        /// <summary>Gets or sets contest QSO transmitted serial number with a value greater than or equal to 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Integer. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_STX">Normative field definition and constraints.</seealso>
        public string STX { get; set; }

        /// <summary>Gets or sets transmitted contest exchange text; dedicated contest fields take precedence over conflicting exchange text.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_STX_STRING">Normative field definition and constraints.</seealso>
        public string STX_STRING { get; set; }

        /// <summary>Gets or sets QSO Submode  use enumeration values for interoperability.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Enumeration : Submode (function of MODE field's value)</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SUBMODE">Normative field definition and constraints.</seealso>
        public string SUBMODE { get; set; }

        /// <summary>Gets or sets indicates that the QSO information pertains to an SWL report.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Boolean. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_SWL">Normative field definition and constraints.</seealso>
        public string SWL { get; set; }

        /// <summary>Gets or sets Ten-Ten number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_TEN_TEN">Normative field definition and constraints.</seealso>
        public string TEN_TEN { get; set; }

        /// <summary>Gets or sets the UTC end time as HHMM or HHMMSS; without QSO_DATE_OFF the duration is less than 24 hours.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Time. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_TIME_OFF">Normative field definition and constraints.</seealso>
        public string TIME_OFF { get; set; }

        /// <summary>Gets or sets HHMM or HHMMSS in UTC.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Time. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_TIME_ON">Normative field definition and constraints.</seealso>
        public string TIME_ON { get; set; }

        /// <summary>Gets or sets the logging station transmitter power in watts, including zero.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_TX_PWR">Normative field definition and constraints.</seealso>
        public string TX_PWR { get; set; }

        /// <summary>Gets or sets the contacted station's UKSMG member number with a value greater than 0.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: PositiveInteger. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_UKSMG">Normative field definition and constraints.</seealso>
        public string UKSMG { get; set; }

        /// <summary>Gets or sets two US counties in the case where the contacted station is located on a border between two counties, representing counties credited to the QSO for the CQ Magazine USA-CA award program. E.g.  MA,Franklin:MA,Hampshire.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondarySubdivisionList. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_USACA_COUNTIES">Normative field definition and constraints.</seealso>
        public string USACA_COUNTIES { get; set; }

        /// <summary>Gets or sets import-only: use STATE instead.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <remarks>Import-only compatibility field. Standard export requires an explicit replacement; it does not silently discard this property.</remarks>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_VE_PROV">Normative field definition and constraints.</seealso>
        public string VE_PROV { get; set; }

        /// <summary>Gets or sets two or four adjacent 4-character or 6-character locators credited to the contacted station for VUCC.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareList. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_VUCC_GRIDS">Normative field definition and constraints.</seealso>
        public string VUCC_GRIDS { get; set; }

        /// <summary>Gets or sets the contacted station's URL.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. The model does not validate or convert this field.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_WEB">Normative field definition and constraints.</seealso>
        public string WEB { get; set; }

        /// <summary>Gets or sets the contacted station height in meters relative to mean sea level.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_ALTITUDE">Normative field definition and constraints.</seealso>
        public string ALTITUDE { get; set; }

        /// <summary>Gets or sets the contacted station alternate subdivision names and codes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondaryAdministrativeSubdivisionListAlt. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_CNTY_ALT">Normative field definition and constraints.</seealso>
        public string CNTY_ALT { get; set; }

        /// <summary>Gets or sets the UTC date a DCL confirmation was received, subject to DCL_QSL_RCVD.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DCL_QSLRDATE">Normative field definition and constraints.</seealso>
        public string DCL_QSLRDATE { get; set; }

        /// <summary>Gets or sets the UTC date a DCL confirmation was sent, subject to DCL_QSL_SENT.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DCL_QSLSDATE">Normative field definition and constraints.</seealso>
        public string DCL_QSLSDATE { get; set; }

        /// <summary>Gets or sets the DCL confirmation receipt status.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DCL_QSL_RCVD">Normative field definition and constraints.</seealso>
        public string DCL_QSL_RCVD { get; set; }

        /// <summary>Gets or sets the DCL confirmation sending status.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_DCL_QSL_SENT">Normative field definition and constraints.</seealso>
        public string DCL_QSL_SENT { get; set; }

        /// <summary>Gets or sets the eQSL authenticity-guaranteed status; the standard default is U, not assigned automatically by this model.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_EQSL_AG">Normative field definition and constraints.</seealso>
        public string EQSL_AG { get; set; }

        /// <summary>Gets or sets the final 2 or 4 characters of a 10/12-character locator whose first 8 characters are in GRIDSQUARE.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareExt. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_GRIDSQUARE_EXT">Normative field definition and constraints.</seealso>
        public string GRIDSQUARE_EXT { get; set; }

        /// <summary>Gets or sets the UTC date of the latest Hamlog.eu upload.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HAMLOGEU_QSO_UPLOAD_DATE">Normative field definition and constraints.</seealso>
        public string HAMLOGEU_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the Hamlog.eu upload status.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HAMLOGEU_QSO_UPLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string HAMLOGEU_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the UTC date of the latest HamQTH upload.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HAMQTH_QSO_UPLOAD_DATE">Normative field definition and constraints.</seealso>
        public string HAMQTH_QSO_UPLOAD_DATE { get; set; }

        /// <summary>Gets or sets the HamQTH upload status.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_HAMQTH_QSO_UPLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string HAMQTH_QSO_UPLOAD_STATUS { get; set; }

        /// <summary>Gets or sets descriptive information about the contacted station Morse key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MORSE_KEY_INFO">Normative field definition and constraints.</seealso>
        public string MORSE_KEY_INFO { get; set; }

        /// <summary>Gets or sets the contacted station Morse key type.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MORSE_KEY_TYPE">Normative field definition and constraints.</seealso>
        public string MORSE_KEY_TYPE { get; set; }

        /// <summary>Gets or sets the logging station height in meters relative to mean sea level.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Number. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_ALTITUDE">Normative field definition and constraints.</seealso>
        public string MY_ALTITUDE { get; set; }

        /// <summary>Gets or sets the logging station ARRL section.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_ARRL_SECT">Normative field definition and constraints.</seealso>
        public string MY_ARRL_SECT { get; set; }

        /// <summary>Gets or sets the logging station alternate subdivision names and codes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: SecondaryAdministrativeSubdivisionListAlt. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_CNTY_ALT">Normative field definition and constraints.</seealso>
        public string MY_CNTY_ALT { get; set; }

        /// <summary>Gets or sets the logging station DARC district location code, including special DOKs.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_DARC_DOK">Normative field definition and constraints.</seealso>
        public string MY_DARC_DOK { get; set; }

        /// <summary>Gets or sets the final 2 or 4 locator characters corresponding to an 8-character MY_GRIDSQUARE.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: GridSquareExt. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_GRIDSQUARE_EXT">Normative field definition and constraints.</seealso>
        public string MY_GRIDSQUARE_EXT { get; set; }

        /// <summary>Gets or sets descriptive information about the logging station Morse key.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: String. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_MORSE_KEY_INFO">Normative field definition and constraints.</seealso>
        public string MY_MORSE_KEY_INFO { get; set; }

        /// <summary>Gets or sets the logging station Morse key type.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_MORSE_KEY_TYPE">Normative field definition and constraints.</seealso>
        public string MY_MORSE_KEY_TYPE { get; set; }

        /// <summary>Gets or sets the logging station comma-separated POTA park references and optional location suffixes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: POTARefList. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_POTA_REF">Normative field definition and constraints.</seealso>
        public string MY_POTA_REF { get; set; }

        /// <summary>Gets or sets the logging station international WWFF reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: WWFFRef. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_MY_WWFF_REF">Normative field definition and constraints.</seealso>
        public string MY_WWFF_REF { get; set; }

        /// <summary>Gets or sets the contacted station comma-separated POTA park references and optional location suffixes.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: POTARefList. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_POTA_REF">Normative field definition and constraints.</seealso>
        public string POTA_REF { get; set; }

        /// <summary>Gets or sets the UTC date of the latest QRZ.com download.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Date. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QRZCOM_QSO_DOWNLOAD_DATE">Normative field definition and constraints.</seealso>
        public string QRZCOM_QSO_DOWNLOAD_DATE { get; set; }

        /// <summary>Gets or sets the QRZ.com download status.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: Enumeration. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QRZCOM_QSO_DOWNLOAD_STATUS">Normative field definition and constraints.</seealso>
        public string QRZCOM_QSO_DOWNLOAD_STATUS { get; set; }

        /// <summary>Gets or sets the received QSL message as ASCII multiline text with CRLF line breaks.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: MultilineString. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_QSLMSG_RCVD">Normative field definition and constraints.</seealso>
        public string QSLMSG_RCVD { get; set; }

        /// <summary>Gets or sets the contacted station international WWFF reference.</summary>
        /// <value>Raw ADIF text; null when absent. ADIF data type: WWFFRef. Assignment does not validate or convert the value.</value>
        /// <seealso href="https://www.adif.org/318/ADIF_318.htm#QSO_Field_WWFF_REF">Normative field definition and constraints.</seealso>
        public string WWFF_REF { get; set; }
    }
}
