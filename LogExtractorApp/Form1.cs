using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogExtractorApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectExcel_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtExcelPath.Text = ofd.FileName;
                }
            }
        }

        private void btnSelectLogFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "로그 파일들이 위치한 폴더를 선택하세요.";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtLogFolderPath.Text = fbd.SelectedPath;
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            string excelPath = txtExcelPath.Text;
            string logFolderPath = txtLogFolderPath.Text;

            if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
            {
                MessageBox.Show("엑셀 파일을 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(logFolderPath) || !Directory.Exists(logFolderPath))
            {
                MessageBox.Show("로그 폴더를 선택해주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            lblStatus.Text = "분석 중...";
            txtStatusLog.Clear();
            progressBar1.Value = 0;

            try
            {
                // Memory-buffered reading for security compliance
                byte[] excelBytes = await Task.Run(() => File.ReadAllBytes(excelPath));

                string excelDir = Path.GetDirectoryName(excelPath)!;
                string excelBaseName = Path.GetFileNameWithoutExtension(excelPath);
                string outputDir = Path.Combine(excelDir, excelBaseName);

                var parser = new ExcelParser();
                var cases = await Task.Run(() => parser.Parse(excelBytes));

                if (cases.Count == 0)
                {
                    txtStatusLog.AppendText("분석된 테스트 케이스가 없습니다. 시트 명과 형식을 확인하세요." + Environment.NewLine);
                    MessageBox.Show("추출할 테스트 케이스가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                txtStatusLog.AppendText($"총 {cases.Count}개 케이스를 발견했습니다. 추출을 시작합니다..." + Environment.NewLine);

                var logger = new Progress<string>(msg =>
                {
                    txtStatusLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                    txtStatusLog.SelectionStart = txtStatusLog.Text.Length;
                    txtStatusLog.ScrollToCaret();
                });

                var progress = new Progress<int>(v =>
                {
                    progressBar1.Value = v;
                });

                var processor = new LogProcessor();
                await Task.Run(() => processor.Process(logFolderPath, cases, outputDir, logger, progress));

                lblStatus.Text = "완료";
                MessageBox.Show($"모든 작업이 완료되었습니다.\n저장 경로: {outputDir}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                txtStatusLog.AppendText($"[에러] {ex.Message}{Environment.NewLine}");
                MessageBox.Show($"오류 발생: {ex.Message}", "에러", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "에러 발생";
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }
    }
}
